﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using FubuCore;
using ST.Docs;
using ST.Docs.Outline;
using StoryTeller;
using StoryTeller.Grammars.Tables;

namespace Specifications.Fixtures.Docs
{
    public class OutlineGenerationFixture : Fixture
    {
        private readonly IList<string> _outlineLines = new List<string>();
        private string _outlineFile;

        public OutlineGenerationFixture()
        {
            Title = "Outline Generation";
        }

        [Hidden]
        public void Line(string Line)
        {
            _outlineLines.Add(Line);
        }

        public IGrammar TheOutlineFileIs()
        {
            return this["Line"].AsTable("The outline definition file is")
                .Before(() => _outlineLines.Clear())
                .After(() =>
                {
                    _outlineFile = Path.GetTempFileName();
                    new FileSystem().WriteToFlatFile(_outlineFile, writer => _outlineLines.Each(writer.WriteLine));
                });
        }

        public IGrammar TheTopicsReadShouldBe()
        {
            return VerifySetOf(() => OutlineReader.ReadFile(_outlineFile).AllTopicsInOrder())
                .Titled("The topics generated should be")
                .MatchOn(x => x.Key, x => x.Title, x => x.Url)
                .Ordered();
        }

        public IGrammar TheWrittenFilesShouldBe()
        {
            return VerifySetOf(theWrittenFiles)
                .Titled("The files written to the destination directory should be")
                .MatchOn(x => x.Path, x => x.FirstLine, x => x.SecondLine);
        }

        private IEnumerable<OutlineFile> theWrittenFiles()
        {
            var directory = Context.Service<DocSettings>().Root;
            var top = OutlineReader.ReadFile(_outlineFile);

            OutlineWriter.WriteToFiles(directory, top);

            var fileSet = new FileSet
            {
                Include = "*.md;order.txt"
            };

            return new FileSystem().FindFiles(directory, fileSet).Select(file =>
            {
                var outlineFile = new OutlineFile
                {
                    Path = file.PathRelativeTo(directory).Replace(Path.DirectorySeparatorChar, '/'),
                };

                new FileSystem().AlterFlatFile(file, list =>
                {
                    outlineFile.FirstLine = list[0];
                    outlineFile.SecondLine = list[1];
                });

                return outlineFile;
            });
        } 

        public class OutlineFile
        {
            public string Path { get; set; }
            public string FirstLine { get; set; }
            public string SecondLine { get; set; }
        }
    }
}