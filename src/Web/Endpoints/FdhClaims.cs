using System.Data;
using System.IO;
using Dapper;
using GoGoClaimApi.Application.Common.Interfaces;
using GoGoClaimApi.Domain.Entities;
using GoGoClaimApi.Web.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace GoGoClaimApi.Web.Endpoints;

public class FdhClaims : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .MapPost(CreateClaims);
    }

    [AllowAnonymous]
    public async Task<int> CreateClaims(
            IConfiguration config,
            IApplicationDbContext context,
            ISqlConnectionFactory sqlConnectionFactory,
            [FromBody] PostFdhClaimsRequest request,
            CancellationToken cancellationToken
        )
    {
        var storagePath = config.GetValue<string>("StoragePath") ?? throw new Exception("StoragePath Not Defined");
        var fdhTokenUrl = config.GetValue<string>("Fdh:TokenUrl") ?? throw new Exception("Fdh:TokenUrl Not Defined");
        var fdhUser = config.GetValue<string>("Fdh:User") ?? throw new Exception("Fdh:User Not Defined");
        var fdhPasswordHash = config.GetValue<string>("Fdh:PasswordHash") ?? throw new Exception("Fdh:PasswordHash Not Defined");
        var fdhHospitalCode = config.GetValue<string>("Fdh:HospitalCode") ?? throw new Exception("Fdh:HospitalCode Not Defined");

        var folderName = Path.GetRandomFileName();

        var dir = Path.Combine(storagePath, folderName);

        Directory.CreateDirectory(dir);

        await CreatePatTextFileAsync(sqlConnectionFactory, dir, request.visitIds);

        var claimeds = context.Claimeds.Where(c => request.visitIds.Contains(c.VisitId));
        context.Claimeds.RemoveRange(claimeds);

        var newClaimeds = new List<Claimed>();
        foreach (var visitId in request.visitIds)
        {
            var newClaimed = new Claimed
            {
                VisitId = visitId,
                Provider = "FDH",
                Dir = dir,
            };

            newClaimeds.Add(newClaimed);
        }

        await context.Claimeds.AddRangeAsync(newClaimeds, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);
        return 1;
    }

    private async Task GenerateTextFileAsync(DataTable table, string path)
    {
        await using var sw = new StreamWriter(path);
        foreach (DataRow datarow in table.Rows)
        {
            var row = string.Empty;
            foreach (var items in datarow.ItemArray)
            {
                row += items + "|";
            }
            await sw.WriteLineAsync(row.Remove(row.Length - 1, 1));
        }
    }

    private async Task CreatePatTextFileAsync(ISqlConnectionFactory sqlConnectionFactory, string dir, List<string> visitIds)
    {
        await using NpgsqlConnection connection = sqlConnectionFactory.CreateConnection();

        var reader = await connection.ExecuteReaderAsync($"""
            select
            		'11243' "HCODE",
            		t_patient.patient_hn "HN",	
            		clear_format(t_patient.patient_changwat) "CHANGWAT",
            		clear_format(substring(t_patient.patient_amphur from 3 for 2)) "AMPHUR", 
            		f16_get_dob(t_health_family.patient_birthday, t_health_family.patient_birthday_true) "DOB",
            	    clear_format(t_patient.f_sex_id) "SEX",
            	    clear_format(f_patient_marriage_status.r_rp1853_marriage_id) "MARRIAGE",
            		clear_format(f16_get_occupa(t_patient.t_patient_id)) "OCCUPA",
            		clear_format(b_map_rp1853_nation.r_rp1853_nation_id) "NATION",
            		f16_get_person_id(t_health_family.t_health_family_id) "PERSON_ID",	
            		f16_get_namepat(t_patient.t_patient_id)::varchar "NAMEPAT",
            		clear_format(f_patient_prefix.patient_prefix_description) "TITLE",
            		substring(clear_format(t_patient.patient_firstname), 1, 40) "FNAME",
            		substring(clear_format(t_patient.patient_lastname), 1, 40) "LNAME",
            		f16_get_idtype(t_health_family.patient_pid, f16_get_passport_no(t_health_family.t_health_family_id)) "IDTYPE"
            	from t_patient
            	left join t_health_family on t_patient.t_health_family_id = t_health_family.t_health_family_id
            	left join f_patient_prefix on f_patient_prefix.f_patient_prefix_id = t_patient.f_patient_prefix_id
            	left join f_patient_marriage_status on t_patient.f_patient_marriage_status_id = f_patient_marriage_status.f_patient_marriage_status_id
            	left join f_patient_nation on t_patient.f_patient_nation_id = f_patient_nation.f_patient_nation_id
            	left join b_map_rp1853_nation on f_patient_nation.f_patient_nation_id = b_map_rp1853_nation.f_patient_nation_id
            	where exists (
                	select 1
                	from t_visit			
            		where t_visit.t_patient_id = t_patient.t_patient_id
            		and t_visit.t_visit_id = any(@visitIds)			
            		limit 1
            	)
            """,
            new
            {
                visitIds
            });

        var table = new DataTable();
        table.Load(reader);

        var path = Path.Combine(dir, "pat.txt");
        await GenerateTextFileAsync(table, path);
    }
   
}
