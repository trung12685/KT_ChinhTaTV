using System.IO;
using ChinhTa.Core;
using ChinhTa.Desktop;

public static class WordCheckCommandChecks
{
    public static void Run(Action<bool,string> assert)
    {
        int calls=0;
        string folder=Path.Combine(Path.GetDirectoryName(Diagnostics.LogPath)!,"OfficeGroups-"+Guid.NewGuid().ToString("N"));
        OfficeFocusedCheckResult Success(IReadOnlyList<OfficeSuiteCase> suites)
        {calls++;return new(new(suites.Select(s=>new OfficeSuiteResult(s,OfficeSuiteStatus.Pass)).ToArray()),folder);}
        string[][] invalid=
        [
            [],["--word-check"],["--word-check",""],["--word-check","all"],
            ["--word-check","isolated-spacing,"],["--word-check",",isolated-spacing"],
            ["--word-check","isolated-spacing,,technical-text"],["--word-check","isolated-spacing,isolated-spacing"],
            ["--word-check","ISOLATED-SPACING"],["--word-check","isolated-spacing, technical-text"],
            ["--word-check","technical-text\n"],["--word-check","\ttechnical-text"],
            ["--word-check","C:\\private-document.docx"],["--word-check","../private.docx"],
            ["--word-check","--optimized-reader"],["--word-check","technical-text","private.docx"],
            ["--unknown","technical-text"],["private.docx"],["--word-check",new string('x',1024)]
        ];
        foreach(var args in invalid)
        {
            var result=WordCheckCommand.Execute(args,Success);
            assert(result.ExitCode==WordCheckCommand.InvalidArguments && result.Folder is null && calls==0 &&
                !result.Output.Contains("private-document",StringComparison.Ordinal) && !result.Output.Contains("RESULT SELECTED_PASS",StringComparison.Ordinal),
                "Malformed/ambiguous Word-check arguments fail before any suite, without echoing input or opening Word");
        }
        var input=new[]{"--word-check","isolated-spacing,spacing-calibration,technical-text"};
        assert(WordCheckCommand.TryParse(input,out var selected) && selected.Select(s=>s.Id).SequenceEqual(new[]{"isolated-spacing","spacing-calibration","technical-text"}),
            "The command requires explicit known suite IDs and preserves requested suite order");
        input[1]="technical-text";
        assert(selected.Count==3,"Parsed suite selection is immutable even when the caller later changes argument storage");
        var accepted=WordCheckCommand.Execute(["--word-check","isolated-spacing,spacing-calibration,technical-text"],Success);
        assert(accepted.ExitCode==0 && calls==1 && accepted.Folder==folder && accepted.Output.Contains("MODE Legacy",StringComparison.Ordinal) &&
            accepted.Output.Contains("RESULT SELECTED_PASS",StringComparison.Ordinal) && accepted.Output.Contains(OfficeFocusedCheck.Scope,StringComparison.Ordinal) &&
            accepted.Output.Contains("office-groups.txt",StringComparison.Ordinal),
            "Complete requested PASS results return0 with legacy mode, real suite scope and owned report location");
        foreach(var status in new[]{OfficeSuiteStatus.Fail,OfficeSuiteStatus.NotRun})
        {
            var result=WordCheckCommand.Execute(["--word-check","technical-text"],suites=>
                new(new([new(suites[0],status,new InvalidOperationException("PRIVATE-DOCUMENT-CONTENT"))]),folder));
            assert(result.ExitCode==1 && result.Folder==folder && result.Output.Contains("RESULT SELECTED_FAIL",StringComparison.Ordinal) &&
                result.Output.Contains(status==OfficeSuiteStatus.Fail?"FAIL —":"NOT_RUN —",StringComparison.Ordinal) && !result.Output.Contains("PRIVATE-DOCUMENT",StringComparison.Ordinal),
                "FAIL and NOT_RUN remain distinct in output and cannot become a successful command or leak exception content");
        }
        var partial=WordCheckCommand.Execute(["--word-check","isolated-spacing,technical-text"],suites=>new(new([new(suites[0],OfficeSuiteStatus.Pass)]),folder));
        assert(partial.ExitCode==1 && partial.Output.Contains("RESULT INCOMPLETE",StringComparison.Ordinal) && partial.Folder is null,
            "A partial result set cannot claim every requested Word family passed");
        var reordered=WordCheckCommand.Execute(["--word-check","isolated-spacing,technical-text"],suites=>new(new(suites.Reverse().Select(s=>new OfficeSuiteResult(s,OfficeSuiteStatus.Pass)).ToArray()),folder));
        assert(reordered.ExitCode==1,"Mismatched/reordered result coverage fails rather than trusting an unrelated successful report");
        var contradictory=WordCheckCommand.Execute(["--word-check","technical-text"],suites=>new(new([new(suites[0],OfficeSuiteStatus.Pass,new IOException("PRIVATE-DOCUMENT"))]),folder));
        assert(contradictory.ExitCode==1 && !contradictory.Output.Contains("PRIVATE-DOCUMENT",StringComparison.Ordinal),
            "A PASS accompanied by an error is contradictory evidence and cannot earn exit0");
        var unknown=WordCheckCommand.Execute(["--word-check","technical-text"],suites=>new(new([new(suites[0],(OfficeSuiteStatus)99)]),folder));
        assert(unknown.ExitCode==1,"An unknown Word-suite status cannot be treated as completed evidence");
        var failed=WordCheckCommand.Execute(["--word-check","technical-text"],_=>throw new IOException("PRIVATE-DOCUMENT-CONTENT"));
        assert(failed.ExitCode==1 && failed.Folder is null && failed.Output.Contains("RESULT OPERATION_FAILED",StringComparison.Ordinal) && !failed.Output.Contains("PRIVATE-DOCUMENT",StringComparison.Ordinal),
            "Initialization/reporting faults return1 without opening an app window or exposing raw error text");
        var invalidFolder=WordCheckCommand.Execute(["--word-check","technical-text"],suites=>new(new([new(suites[0],OfficeSuiteStatus.Pass)]),"PRIVATE-DOCUMENT-PATH"));
        assert(invalidFolder.ExitCode==1 && invalidFolder.Folder is null && !invalidFolder.Output.Contains("PRIVATE-DOCUMENT",StringComparison.Ordinal),
            "An unowned report path is neither printed nor accepted as complete run evidence");
        assert(WordCheckCommand.TryParse(["--word-check","outline-references"],out var references) && references.Count==1 &&
            references[0]==OfficeFocusedCheck.Cases.Single(s=>s.Id=="outline-references") && WordCheckCommand.Usage.Contains("outline-references",StringComparison.Ordinal),
            "The independent reference suite is explicitly selectable by its own command ID without selecting Subject, Line or another Word family");
        foreach(var status in new[]{OfficeSuiteStatus.Pass,OfficeSuiteStatus.Fail,OfficeSuiteStatus.NotRun})
        {
            var result=WordCheckCommand.Execute(["--word-check","outline-references"],suites=>
                new(new([new(suites[0],status)]),folder));
            assert(result.ExitCode==(status==OfficeSuiteStatus.Pass?0:1) && result.Folder==folder &&
                result.Output.Contains("SELECTED outline-references",StringComparison.Ordinal) &&
                result.Output.Contains((status==OfficeSuiteStatus.Pass?"PASS":status==OfficeSuiteStatus.Fail?"FAIL":"NOT_RUN")+" — "+references[0].Name,StringComparison.Ordinal) &&
                result.Output.Contains(OfficeFocusedCheck.Scope,StringComparison.Ordinal),
                "The independent reference command preserves its actual PASS/FAIL/NOT_RUN and limited report scope instead of borrowing a full-integration result");
        }
    }
}
