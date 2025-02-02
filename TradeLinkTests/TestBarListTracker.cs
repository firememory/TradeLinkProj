﻿using System;
using System.Collections.Generic;
using TradeLink.Common;
using NUnit.Framework;
using TradeLink.API;

namespace TestTradeLink
{
#if DEBUG
    // for speeding up debugger-based unit test runs
    // all tests marked as explicit only temporarily unmark for test being debugged
    [TestFixture, Explicit]
    //[TestFixture]
#else
    [TestFixture]
#endif
    public class TestBarListTracker
    {
        public TestBarListTracker() { }

        [Test]
        public void DefaultInt()
        {
            BarListTracker blt = new BarListTracker();
            blt.GotNewBar+=new SymBarIntervalDelegate(blt_GotNewBar);

            Tick [] tape = TestBarList.SampleData();
            // get second tape and change symbol
            Tick[] tape2 = TestBarList.SampleData();
            for (int i = 0; i<tape2.Length; i++)
                tape2[i].symbol = "TST2";

            // add ticks from both tape to tracker
            for (int i = 0; i<tape.Length; i++)
            {
                blt.newTick(tape[i]);
                blt.newTick(tape2[i]);
            }

            //make sure we got two symbols as bar events
            Assert.AreEqual(2, syms.Count);
            // make sure our symbols matched barlist count
            Assert.AreEqual(blt.SymbolCount, syms.Count);

            int secondcount = 0;
            string symstring = string.Empty;
            foreach (string sym in blt)
            {
                secondcount++;
                symstring += sym;
            }

            // make sure enumeration equals symbol count
            Assert.AreEqual(syms.Count, secondcount);
            // make sure symbols are there
            Assert.IsTrue(symstring.Contains("TST") && symstring.Contains("TST2"));

            // change default interval
            blt.DefaultInterval = BarInterval.Minute;
            // make sure same on individual bars
            Assert.AreEqual(blt.DefaultInterval,blt["TST"].DefaultInterval);

            Assert.AreEqual(9, blt["TST"].IntervalCount(BarInterval.Minute));
            Assert.AreEqual(3, blt["TST"].IntervalCount(BarInterval.FiveMin));

        }

        List<string> syms = new List<string>();
        void  blt_GotNewBar(string symbol, int interval)
        {
            if (!syms.Contains(symbol))
                syms.Add(symbol);
        }


        [Test]
        public void TickInt()
        {
            Tick[] tape = TestBarList.SampleData();
            BarListTracker blt = new BarListTracker(new[] { 3 }, new BarInterval[] { BarInterval.CustomTicks });

            foreach (Tick k in tape)
                blt.newTick(k);

            Assert.AreEqual(4,blt[tape[0].symbol].Count);
        }


#if DEBUG
         [Test, Explicit]
        //[Test]
#else
        [Test]
#endif
        public void DefaultIntervalsAndInitilizedFields()
        {


            var blt_t300 = new BarListTracker(new int[] { 300 }, new BarInterval[] { BarInterval.CustomTime });
            var blt_v10000 = new BarListTracker(new int[] { 10000}, new BarInterval[] { BarInterval.CustomVol});
            g.d(blt_v10000.DefaultInterval.ToString());
            g.d(blt_t300.DefaultInterval.ToString());
            Assert.AreEqual(blt_v10000.Intervals.Length, blt_v10000.CustomIntervals.Length, "vol mismatch custom/intervals");
            Assert.AreEqual(10000, blt_v10000.CustomIntervals[0], "vol wrong interval size");
            Assert.AreEqual(BarInterval.CustomVol, blt_v10000.Intervals[0], "vol wrong interval type");

            Assert.AreEqual(blt_t300.Intervals.Length, blt_t300.CustomIntervals.Length,"mismatch custom/intervals");
            Assert.AreEqual(300, blt_t300.CustomIntervals[0], "wrong interval size");
            
            Assert.AreEqual(BarInterval.CustomTime,blt_t300.Intervals[0] , "wrong interval type");
            
        }


    }
}
