using ChinhTa.WordProtocol;
using ChinhTa.WordWorker;

internal static class TableChecks
{
    internal static void Run(WorkerService service,Action<bool,string> assert)
    {
        BridgeResponse Scan(params ParagraphDto[] rows)=>service.Handle(new(){Id=Guid.NewGuid().ToString(),Operation="scan",DocumentId=Guid.NewGuid().ToString(),SnapshotHash=BridgeContract.Hash("synthetic table"),Paragraphs=rows.ToList()});
        ParagraphDto Row(string text,bool table=true)=>new(){Index=1,Start=100,End=100+text.Length-1,Text=text,InTable=table};
        var row=Row("Mục\tCần sử lý.\r\a");var response=Scan(row);
        assert(response.Success && response.Findings.Count==1 && response.Findings[0].Offset==row.Text.IndexOf("sử",StringComparison.Ordinal) && response.Findings[0].InTable,"A finding after a tab in a table retains the exact string offset including the tab");
        assert(response.TotalParagraphs==1 && response.ScannedParagraphs==1 && response.ScannedTableParagraphs==1 && response.ScannedSegments==2,"The returned scope reports the paragraphs, table paragraphs and text segments actually checked");
        response=Scan(Row("bổ\txung\r\a"));assert(response.Success && response.Findings.Count==0,"Words separated by a table/tab boundary cannot form a false bổ xung phrase");
        response=Scan(Row("sử\alý\r\a"));assert(response.Findings.Count==0,"A cell marker is a boundary and cannot form a false sử lý phrase across cells");
        response=Scan(Row("Mục. \tCần sử lý.\r\a"));assert(response.Findings.Count==1 && response.Findings[0].Original=="sử","Alignment whitespace before a tab cannot become a fabricated end-of-paragraph warning");
        foreach(char unsupported in new[]{'\u0013','\u0014','\u0015','\u0001','\ufffc'})
        {
            response=Scan(Row("Cần sử lý."+unsupported+"DATE\r\a"));
            assert(response.Success && response.ScannedParagraphs==0 && response.SkippedParagraphs==1 && response.Findings.Count==0,"An unsupported field/object paragraph is explicitly outside the checked scope: "+((int)unsupported).ToString("X4"));
        }
        row=Row("Cần sử lý.\vCần sử lý.\r\a");response=Scan(row);
        assert(response.Findings.Count==2 && response.Findings[1].Offset>row.Text.IndexOf('\v'),"A manual line break separates analysis while retaining the second line's source offset");
        var first=Row("Cần sử lý.\r\a");var second=Row("Cần sử lý.\r\a");second.Index=2;second.Start=1000;second.End=1012;
        response=Scan(first,second);assert(response.Findings.Count==2 && response.Findings.Select(f=>f.ParagraphIndex).SequenceEqual(new[]{1,2}),"Identical cell text remains tied to distinct paragraph identities");
        row=Row("Cần sử lý.\r\a");row.Start=-1;response=Scan(row);assert(response.SkippedParagraphs==1 && response.ScannedParagraphs==0,"Invalid native paragraph bounds are skipped and cannot claim checked coverage");
        var many=Enumerable.Range(1,1100).Select(i=>new ParagraphDto{Index=i,Start=i*20,End=i*20+12,Text="Cần sử lý.\r\a",InTable=true}).ToArray();response=Scan(many);
        assert(response.Success && response.Truncated && response.Findings.Count==1000 && response.ScannedParagraphs<response.TotalParagraphs,"Reaching the finding cap is explicit and cannot claim that the remaining table rows were checked");
    }
}
