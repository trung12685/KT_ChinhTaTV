using System.Text.RegularExpressions;
using ChinhTa.WordProtocol;
namespace ChinhTa.WordWorker;
internal static class DeepReview
{
    private static readonly Regex Dates=new(@"(?:(?<weekday>thứ\s+(?:hai|ba|tư|năm|sáu|bảy)|chủ\s+nhật)\s*,?\s*)?ngày\s+(?<day>[0-9]{1,2})(?:\s*/\s*(?<month>[0-9]{1,2})\s*/\s*|\s+tháng\s+(?<month>[0-9]{1,2})\s+năm\s+)(?<year>[0-9]{4})\b",RegexOptions.IgnoreCase|RegexOptions.CultureInvariant|RegexOptions.NonBacktracking);
    internal static IEnumerable<FindingDto> Check(string text)
    {
        foreach(Match match in Dates.Matches(text))
        {
            int day=int.Parse(match.Groups["day"].Value),month=int.Parse(match.Groups["month"].Value),year=int.Parse(match.Groups["year"].Value);
            bool valid=year>=1 && year<=9999 && month>=1 && month<=12 && day>=1 && day<=DateTime.DaysInMonth(year,month);
            string? message=valid?null:"Ngày/tháng này không tồn tại trong lịch; đối chiếu lại mốc thời gian, chưa tự đoán ngày đúng.";
            if(valid && match.Groups["weekday"].Success)
            {
                string actual=Regex.Replace(match.Groups["weekday"].Value.ToLowerInvariant(),@"\s+"," ");
                string expected=new[]{"chủ nhật","thứ hai","thứ ba","thứ tư","thứ năm","thứ sáu","thứ bảy"}[(int)new DateTime(year,month,day).DayOfWeek];
                if(actual!=expected)message="Thứ và ngày/tháng/năm chưa khớp: ngày này là "+expected+". Đối chiếu ý định trước khi sửa thứ hoặc ngày.";
            }
            if(message is not null)yield return new(){Offset=match.Index,Length=match.Length,Original=match.Value,Category="Context",Message=message};
        }
    }
}
