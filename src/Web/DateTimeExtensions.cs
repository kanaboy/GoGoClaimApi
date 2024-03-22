namespace GoGoClaimApi.Web;

public static class DateTimeExtensions
{
    public static DateTime ToStartDateTime(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 0, 0, 0);
    }

    public static DateTime ToEndDateTime(this DateTime dateTime)
    {
        return new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, 23, 59, 59);
    }
    public static string ToDateString(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd");
    }

    public static string ToStartDateTimeString(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd,00:00:00");
    }

    public static string ToEndDateTimeString(this DateTime dateTime)
    {
        return dateTime.ToString("yyyy-MM-dd,23:59:59");
    }

    public static string ToBuddhistStartDateTimeString(this DateTime christian, string splitTimeWith = ",")
    {
        return christian.ToBuddhistDateTimeString(splitTimeWith, "00:00:00");
    }

    public static string ToBuddhistEndDateTimeString(this DateTime christian, string splitTimeWith = ",")
    {
        return christian.ToBuddhistDateTimeString(splitTimeWith, "23:59:59");
    }

    /// <summary>
    /// Date Only HH:mm:ss
    /// </summary>
    /// <param name="christian"></param>
    /// <returns></returns>
    public static string ToBuddhistDateTimeString(this DateTime christian)
    {
        return christian.ToBuddhistDateTimeString(",", christian.ToString("HH:mm:ss"));
    }

    public static string ToBuddhistDateTimeString(this DateTime christian, string splitTimeWith)
    {
        return christian.ToBuddhistDateTimeString(splitTimeWith, christian.ToString("HH:mm:ss"));
    }

    public static string ToBuddhistDateTimeString(this DateTime christian, string splitTimeWith, string timeFormat)
    {
        return $"{christian.Year + 543}-{christian.Month.ToString().PadLeft(2, '0')}-{christian.Day}{splitTimeWith}{timeFormat}";
    }
}
