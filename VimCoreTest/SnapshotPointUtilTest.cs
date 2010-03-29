﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Microsoft.VisualStudio.Text;
using Vim;

namespace VimCoreTest
{
    [TestFixture]
    public class SnapshotPointUtilTest
    {
        static string[] s_lines = new string[]
            {
                "summary description for this line",
                "some other line",
                "running out of things to make up"
            };

        ITextBuffer _buffer = null;
        ITextSnapshot _snapshot = null;

        public void Create(params string[] lines)
        {
            _buffer = Utils.EditorUtil.CreateBuffer(lines);
            _snapshot = _buffer.CurrentSnapshot;
        }

        [TearDown]
        public void TearDown()
        {
            _buffer = null;
            _snapshot = null;
        }

        [Test]
        public void GetLineRangeSpan1()
        {
            var span = SnapshotPointUtil.GetLineRangeSpan(new SnapshotPoint(_snapshot,0), 1);
            var line = _snapshot.GetLineFromLineNumber(0);
            Assert.AreEqual(line.Extent, span);
        }

        /// <summary>
        /// Multi-line range
        /// </summary>
        [Test]
        public void GetLineRangeSpan2()
        {
            var span = SnapshotPointUtil.GetLineRangeSpan(new SnapshotPoint(_snapshot, 0), 2);
            var start = _snapshot.GetLineFromLineNumber(0);
            var second = _snapshot.GetLineFromLineNumber(1);
            var expected = new Span(start.Start, second.End - start.Start);
            Assert.AreEqual(span.Span, expected);
        }

        [Test]
        public void GetLineRangeSpanIncludingLineBreak1()
        {
            Create("foo", "bar");
            var span = SnapshotPointUtil.GetLineRangeSpanIncludingLineBreak(new SnapshotPoint(_snapshot, 0), 1);
            Assert.AreEqual(_snapshot.GetLineFromLineNumber(0).ExtentIncludingLineBreak, span);
        }

        [Test]
        public void GetCharacterSpan1()
        {
            Create("foo");
            var span = SnapshotPointUtil.GetCharacterSpan(new SnapshotPoint(_buffer.CurrentSnapshot, 0));
            Assert.AreEqual(0, span.Start.Position);
            Assert.AreEqual(1, span.Length);
        }

        [Test, Description("Empty line shtould have a character span of the entire line")]
        public void GetCharacterSpan2()
        {
            Create("foo", String.Empty, "baz");
            var line = _buffer.CurrentSnapshot.GetLineFromLineNumber(1);
            var span = SnapshotPointUtil.GetCharacterSpan(line.Start);
            Assert.AreEqual(span, line.ExtentIncludingLineBreak);
        }

        [Test, Description("End of line should have the span of the line break")]
        public void GetCharacterSpan3()
        {
            Create("foo", "bar");
            var line = _buffer.CurrentSnapshot.GetLineFromLineNumber(0);
            var span = SnapshotPointUtil.GetCharacterSpan(line.End);
            Assert.AreEqual(span, new SnapshotSpan(line.End, line.EndIncludingLineBreak));
        }

        [Test]
        public void GetNextPointWithWrap1()
        {
            Create("foo", "baz");
            var line = _buffer.CurrentSnapshot.GetLineFromLineNumber(0);
            var next = SnapshotPointUtil.GetNextPointWithWrap(line.Start);
            Assert.AreEqual(1, next.Position);
        }

        [Test, Description("End of line should wrap")]
        public void GetNextPointWithWrap2()
        {
            Create("foo", "bar");
            var line = _buffer.CurrentSnapshot.GetLineFromLineNumber(0);
            var next = SnapshotPointUtil.GetNextPointWithWrap(line.End);
            line = _buffer.CurrentSnapshot.GetLineFromLineNumber(1);
            Assert.AreEqual(line.Start, next);
        }

        [Test, Description("Wrap around the buffer")]
        public void GetNextPointWithWrap3()
        {
            Create("foo", "bar");
            var next = SnapshotPointUtil.GetNextPointWithWrap(_buffer.CurrentSnapshot.GetLineFromLineNumber(1).End);
            Assert.AreEqual(_buffer.CurrentSnapshot.GetLineFromLineNumber(0).Start, next);
        }
        [Test]
        public void GetNextPoint1()
        {
            Create("foo", "baz");
            var line = _buffer.CurrentSnapshot.GetLineFromLineNumber(0);
            var next = SnapshotPointUtil.GetNextPoint(line.Start);
            Assert.AreEqual(1, next.Position);
        }

        [Test, Description("End of line should wrap")]
        public void GetNextPoint2()
        {
            Create("foo", "bar");
            var line = _buffer.CurrentSnapshot.GetLineFromLineNumber(0);
            var next = SnapshotPointUtil.GetNextPoint(line.End);
            line = _buffer.CurrentSnapshot.GetLineFromLineNumber(1);
            Assert.AreEqual(line.Start, next);
        }

        [Test, Description("Don't around the buffer")]
        public void GetNextPoint3()
        {
            Create("foo", "bar");
            var point = _buffer.CurrentSnapshot.GetLineFromLineNumber(1).End;
            var next = SnapshotPointUtil.GetNextPoint(point);
            Assert.AreEqual(next, point);
        }

        [Test]
        public void GetPreviousPointWithWrap1()
        {
            Create("foo", "bar");
            var prev = SnapshotPointUtil.GetPreviousPointWithWrap(new SnapshotPoint(_buffer.CurrentSnapshot, 1));
            Assert.AreEqual(_buffer.CurrentSnapshot.GetLineFromLineNumber(0).Start, prev);
        }

        [Test]
        public void GetPreviousPointWithWrap2()
        {
            Create("foo", "bar");
            var prev = SnapshotPointUtil.GetPreviousPointWithWrap(_buffer.CurrentSnapshot.GetLineFromLineNumber(1).Start);
            Assert.AreEqual(_buffer.CurrentSnapshot.GetLineFromLineNumber(0).End, prev);
        }

        [Test]
        public void GetPreviousPointWithWrap3()
        {
            Create("foo", "bar");
            var prev = SnapshotPointUtil.GetPreviousPointWithWrap(new SnapshotPoint(_buffer.CurrentSnapshot, 0));
            Assert.AreEqual(SnapshotUtil.GetEndPoint(_buffer.CurrentSnapshot), prev);
        }

        [Test]
        public void GetLines1()
        {
            Create("foo", "bar");
            var point = new SnapshotPoint(_snapshot, 0);
            var agg = SnapshotPointUtil.GetLines(point, SearchKind.Forward).Select(x => x.GetText()).Aggregate((x, y) => x + y);
            Assert.AreEqual("foobar", agg);
        }

        /// <summary>
        /// Check forward wraping
        /// </summary>
        [Test]
        public void GetLines2()
        {
            Create("foo", "bar", "baz");
            var point = new SnapshotPoint(_snapshot, 6);
            var agg = SnapshotPointUtil.GetLines(point, SearchKind.Forward)
                .Select(x => x.GetText())
                .Aggregate((x, y) => x + y);
            Assert.AreEqual("barbaz", agg);
            var point2 = new SnapshotPoint(_snapshot, 6);
            agg = SnapshotPointUtil.GetLines(point2, SearchKind.ForwardWithWrap)
                .Select(x => x.GetText())
                .Aggregate((x, y) => x + y);
            Assert.AreEqual("barbazfoo", agg);
        }

        [Test]
        public void GetLines3()
        {
            Create("foo bar", "baz");
            var line = _snapshot.GetLineFromLineNumber(1);
            var list = SnapshotPointUtil.GetLines(line.Start.Subtract(1), SearchKind.Backward);
            Assert.AreEqual(1, list.Count());
        }

        [Test]
        public void GetLines4()
        {
            Create("abcde".Select(x => x.ToString()).ToArray());
            var line = _snapshot.GetLineFromLineNumber(2);
            var msg = SnapshotPointUtil.GetLines(line.Start, SearchKind.Backward).Select(x => x.GetText()).Aggregate((x, y) => x + y);
            Assert.AreEqual("cba", msg);
        }

        [Test]
        public void GetLines5()
        {
            Create("abcde".Select(x => x.ToString()).ToArray());
            var line = _snapshot.GetLineFromLineNumber(2);
            var msg = SnapshotPointUtil.GetLines(line.Start, SearchKind.Forward).Select(x => x.GetText()).Aggregate((x, y) => x + y);
            Assert.AreEqual("cde", msg);
        }

        [Test]
        public void GetLines6()
        {
            Create("abcde".Select(x => x.ToString()).ToArray());
            var line = _snapshot.GetLineFromLineNumber(2);
            var msg = SnapshotPointUtil.GetLines(line.Start, SearchKind.BackwardWithWrap).Select(x => x.GetText()).Aggregate((x, y) => x + y);
            Assert.AreEqual("cbaed", msg);
        }

        [Test]
        public void GetLines7()
        {
            Create("abcde".Select(x => x.ToString()).ToArray());
            var line = _snapshot.GetLineFromLineNumber(2);
            var msg = SnapshotPointUtil.GetLines(line.Start, SearchKind.ForwardWithWrap).Select(x => x.GetText()).Aggregate((x, y) => x + y);
            Assert.AreEqual("cdeab", msg);
        }

        [Test]
        public void GetSpans1()
        {
            Create("foo", "bar");
            var point = new SnapshotPoint(_snapshot, 1);
            var list = SnapshotPointUtil.GetSpans(point, SearchKind.ForwardWithWrap).Select(x => x.GetText()).ToList();
            Assert.AreEqual("oo", list[0]);
            Assert.AreEqual("bar", list[1]);
            Assert.AreEqual("f", list[2]);
        }

        [Test]
        public void GetSpans2()
        {
            Create("foo", "bar");
            var point = new SnapshotPoint(_snapshot, 1);
            var list = SnapshotPointUtil.GetSpans(point, SearchKind.BackwardWithWrap).Select(x => x.GetText()).ToList();
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("f", list[0]);
            Assert.AreEqual("bar", list[1]);
            Assert.AreEqual("oo", list[2]);
        }

        [Test, Description("Full lines starting at line not 0")]
        public void GetSpans3()
        {
            Create("foo", "bar baz");
            var line = _snapshot.GetLineFromLineNumber(1);
            var list = SnapshotPointUtil.GetSpans(line.Start, SearchKind.ForwardWithWrap).Select(x => x.GetText()).ToList();
            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("bar baz", list[0]);
            Assert.AreEqual("foo", list[1]);
        }

        [Test, Description("Don't wrap if we say dont't wrap")]
        public void GetSpans4()
        {
            Create("foo");
            var line = _snapshot.GetLineFromLineNumber(0);
            var list = SnapshotPointUtil.GetSpans(line.End, SearchKind.Forward);
            Assert.AreEqual(1, list.Count());
        }

        [Test, Description("Don't wrap backwards if we don't say wrap")]
        public void GetSpans5()
        {
            Create("foo");
            var line = _snapshot.GetLineFromLineNumber(0);
            var list = SnapshotPointUtil.GetSpans(line.Start + 2, SearchKind.Backward);
            Assert.AreEqual(1, list.Count());
        }

        [Test, Description("Multi lack of wrap")]
        public void GetSpans6()
        {
            Create("foo", "bar", "baz");
            var line = _snapshot.GetLineFromLineNumber(1);
            var list = SnapshotPointUtil.GetSpans(line.Start + 1, SearchKind.Forward);
            Assert.AreEqual(2, list.Count());
        }

        [Test, Description("multi lack of wrap reverse")]
        public void GetSpans7()
        {
            Create("foo bar", "baz");
            var line = _snapshot.GetLineFromLineNumber(1);
            var list = SnapshotPointUtil.GetSpans(line.Start, SearchKind.Backward);
            Assert.AreEqual(2, list.Count());
        }

        [Test]
        public void GetSpans8()
        {
            Create("foo bar", "baz");
            var line = _snapshot.GetLineFromLineNumber(1);
            var list = SnapshotPointUtil.GetSpans(line.Start.Subtract(1), SearchKind.Backward);
            Assert.AreEqual(1, list.Count());
        }

        [Test, Description("Handle being given a point in the middle of a line break")]
        public void GetSpans9()
        {
            Create("foo", "bar");
            var point = _snapshot.GetLineFromLineNumber(0).End.Add(1);
            var list = SnapshotPointUtil.GetSpans(point, SearchKind.ForwardWithWrap).Select(x => x.GetText());
            Assert.AreEqual(3, list.Count());
            Assert.AreEqual(String.Empty, list.ElementAt(0));
            Assert.AreEqual("bar", list.ElementAt(1));
            Assert.AreEqual("foo", list.ElementAt(2));
        }

     

    }
}