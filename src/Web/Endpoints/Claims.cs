using Dapper;
using GoGoClaimApi.Application.Common.Interfaces;
using GoGoClaimApi.Web.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace GoGoClaimApi.Web.Endpoints;
public class Claims : EndpointGroupBase
{
    public override void Map(WebApplication app)
    {
        app.MapGroup(this)
            .RequireAuthorization()
            .MapGet(GetClaims);
    }

    [AllowAnonymous]
    public async Task<ClaimResponse> GetClaims(
            ISqlConnectionFactory sqlConnectionFactory,
            [FromQuery] string visitDateRange = "",
            [FromQuery] string[]? claimStatuses = null,
            [FromQuery] string visitType = "",
            [FromQuery] string visitStatus = "",
            [FromQuery] string mainInscl = "",
            [FromQuery] string q = "",
            [FromQuery] int page = 0,
            [FromQuery] int itemsPerPage = 25
        )
    {
        var visitDateRangeSplits = visitDateRange != "" ? visitDateRange.Split(" to ") : [DateTime.Now.ToDateString(), DateTime.Now.ToDateString()];
       
        var startDate = DateTime.Parse(visitDateRangeSplits[0]).ToBuddhistStartDateTimeString();
        var endDate = DateTime.Parse(visitDateRangeSplits[1]).ToBuddhistEndDateTimeString();

        await using NpgsqlConnection connection = sqlConnectionFactory.CreateConnection();

        var whereVisitDateTime = "v.visit_begin_visit_time between @startDate and @endDate";
        if(visitType == "1")
            whereVisitDateTime = "v.visit_staff_doctor_discharge_date_time between @startDate and @endDate";

        var andWhereClaimStatuses = "";
        if (claimStatuses != null && claimStatuses.Length == 1 && claimStatuses[0] == "0")
            andWhereClaimStatuses = "and not exists (select 1 from gogoclaim.\"Claimeds\" c where c.\"VisitId\" = v.t_visit_id)";
        else if (claimStatuses != null && claimStatuses.Length == 1 && claimStatuses[0] == "1")
            andWhereClaimStatuses = "and exists (select 1 from gogoclaim.\"Claimeds\" c where c.\"VisitId\" = v.t_visit_id)";

        var andWhereVisitType = "and v.f_visit_type_id in ('0', '1')";
        if (visitType != "")
            andWhereVisitType = "and v.f_visit_type_id = @visitType";

        var andWhereVisitStatus = "and v.f_visit_status_id in ('1', '2', '3', '4')";
        if (visitStatus != "" && visitStatus == "3")
            andWhereVisitStatus = "and v.f_visit_status_id = '3'";
        else if (visitStatus != "" && visitStatus != "3")
            andWhereVisitStatus = "and v.f_visit_status_id in ('1', '2', '4')";

        var andWhereMainInscl = "and is_not_empty(f16_get_maininscl(v.t_visit_id))";
        if (mainInscl != "")
            andWhereMainInscl = "and f16_get_maininscl(v.t_visit_id) = @mainInscl";
        
        q = q.Trim();
        var andWhereQ = "";
        if (q != "")
        {
            var orWhereQFirstnameOrLastname = @$"
                    or p.patient_firstname ilike '%{q}%'
                    or p.patient_lastname ilike '%{q}%'
                ";
            if (q.Contains(' '))
            {
                var qSplits = q.Split(' ');
                var text1 = qSplits[0];
                var text2 = qSplits[1];
                orWhereQFirstnameOrLastname = @$"
                        or (p.patient_firstname ilike '%{text1}%' 
                        and p.patient_lastname ilike '%{text2}%')
                    ";
            }
            andWhereQ = @$"and (
                    p.patient_hn = '{q.PadLeft(9, '0')}'
                    or v.visit_vn = '{q}'
                    {orWhereQFirstnameOrLastname}
                )";
        }
            
        var totalClaims = await connection.ExecuteScalarAsync<long>($"""
                select count(*) 
                from public.t_visit v
                inner join public.t_patient p on v.t_patient_id = p.t_patient_id
                where {whereVisitDateTime}
                {andWhereClaimStatuses}
                {andWhereVisitType}
                {andWhereVisitStatus}
                {andWhereMainInscl}
                {andWhereQ}
            """,
            new { 
                startDate, 
                endDate,
                visitType,
                mainInscl
            });

        var totalOpds = await connection.ExecuteScalarAsync<long>($"""
                select count(*) 
                from public.t_visit v
                inner join public.t_patient p on v.t_patient_id = p.t_patient_id
                where {whereVisitDateTime}
                {andWhereClaimStatuses}
                {andWhereVisitType}
                {andWhereVisitStatus}
                {andWhereMainInscl}
                {andWhereQ}
                and v.f_visit_type_id = '0'
            """,
           new
           {
               startDate,
               endDate,
               visitType,
               mainInscl
           });

        var totalIpds = await connection.ExecuteScalarAsync<long>($"""
                select count(*) 
                from public.t_visit v
                inner join public.t_patient p on v.t_patient_id = p.t_patient_id
                where {whereVisitDateTime}
                {andWhereClaimStatuses}
                {andWhereVisitType}
                {andWhereVisitStatus}
                {andWhereMainInscl}
                {andWhereQ}
                and v.f_visit_type_id = '1'
            """,
         new
         {
             startDate,
             endDate,
             visitType,
             mainInscl
         });

        var totalCompletedVisits = await connection.ExecuteScalarAsync<long>($"""
                            select count(*) 
                            from public.t_visit v
                            inner join public.t_patient p on v.t_patient_id = p.t_patient_id
                            where {whereVisitDateTime}
                            {andWhereClaimStatuses}
                            {andWhereVisitType}
                            {andWhereVisitStatus}
                            {andWhereMainInscl}
                            {andWhereQ}
                            and v.f_visit_status_id = '3'
                        """,
             new
             {
                 startDate,
                 endDate,
                 visitType,
                 mainInscl
             });

        var totalUncompletedVisits = await connection.ExecuteScalarAsync<long>($"""
                                select count(*) 
                                from public.t_visit v
                                inner join public.t_patient p on v.t_patient_id = p.t_patient_id
                                where {whereVisitDateTime}
                                {andWhereClaimStatuses}
                                {andWhereVisitType}
                                {andWhereVisitStatus}
                                {andWhereMainInscl}
                                {andWhereQ}
                                and v.f_visit_status_id in ('1', '2', '4')
                            """,
            new
            {
                startDate,
                endDate,
                visitType,
                mainInscl
            });

        var totalUcss = await connection.ExecuteScalarAsync<long>($"""
                                select count(*) 
                                from public.t_visit v
                                inner join public.t_patient p on v.t_patient_id = p.t_patient_id
                                where {whereVisitDateTime}
                                {andWhereClaimStatuses}
                                {andWhereVisitType}
                                {andWhereVisitStatus}
                                {andWhereMainInscl}
                                {andWhereQ}
                                and f16_get_maininscl(v.t_visit_id) = 'UCS'
                            """,
            new
            {
                startDate,
                endDate,
                visitType,
                mainInscl
            });

        var totalOfcs = await connection.ExecuteScalarAsync<long>($"""
                                select count(*) 
                                from public.t_visit v
                                inner join public.t_patient p on v.t_patient_id = p.t_patient_id
                                where {whereVisitDateTime}
                                {andWhereClaimStatuses}
                                {andWhereVisitType}
                                {andWhereVisitStatus}
                                {andWhereMainInscl}
                                {andWhereQ}
                                and f16_get_maininscl(v.t_visit_id) = 'OFC'
                            """,
      new
      {
          startDate,
          endDate,
          visitType,
          mainInscl
      });

        var totalLgos = await connection.ExecuteScalarAsync<long>($"""
                                select count(*) 
                                from public.t_visit v
                                inner join public.t_patient p on v.t_patient_id = p.t_patient_id
                                where {whereVisitDateTime}
                                {andWhereClaimStatuses}
                                {andWhereVisitType}
                                {andWhereVisitStatus}
                                {andWhereMainInscl}
                                {andWhereQ}
                                and f16_get_maininscl(v.t_visit_id) = 'LGO'
                            """,
      new
      {
          startDate,
          endDate,
          visitType,
          mainInscl
      });

        var totalStps = await connection.ExecuteScalarAsync<long>($"""
                                select count(*) 
                                from public.t_visit v
                                inner join public.t_patient p on v.t_patient_id = p.t_patient_id
                                where {whereVisitDateTime}
                                {andWhereClaimStatuses}
                                {andWhereVisitType}
                                {andWhereVisitStatus}
                                {andWhereMainInscl}
                                {andWhereQ}
                                and f16_get_maininscl(v.t_visit_id) = 'STP'
                            """,
      new
      {
          startDate,
          endDate,
          visitType,
          mainInscl
      });

        var claims = await connection.QueryAsync($"""
                select
                    v.t_visit_id "id",
                    (select 1 from gogoclaim."Claimeds" c where c."VisitId" = v.t_visit_id) "isClaimed",
                    get_date_time_ad(v.visit_staff_doctor_discharge_date_time) "dischargeDateTime",
                    p.patient_hn "hn",
                    concat(pf.patient_prefix_description, p.patient_firstname, ' ', p.patient_lastname) "fullname",
                    v.visit_vn "vn",
                    v.f_visit_type_id "visitTypeId",
                    f16_get_maininscl(v.t_visit_id) "mainInscl",
                    v.f_visit_status_id "visitStatus"
                from public.t_visit v
                inner join public.t_patient p on v.t_patient_id = p.t_patient_id
                left join public.f_patient_prefix pf on p.f_patient_prefix_id = pf.f_patient_prefix_id
                where {whereVisitDateTime}
                {andWhereClaimStatuses}
                {andWhereVisitType}
                {andWhereVisitStatus}
                {andWhereMainInscl}
                {andWhereQ}
                limit @itemsPerPage offset @itemsPerPageXPage
            """, 
            new { 
                startDate, 
                endDate,
                visitType,
                mainInscl,
                itemsPerPage, itemsPerPageXPage = itemsPerPage * (page > 0 ? page - 1 : page)
            });

        return new ClaimResponse
        {
            claims = claims,
            totalClaims = totalClaims,
            totalWidgets1 = [totalOpds, totalIpds, totalCompletedVisits, totalUncompletedVisits],
            totalWidgets2 = [totalUcss, totalOfcs, totalLgos, totalStps]
        };
    }
}
