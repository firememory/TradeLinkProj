﻿using System;
using System.Collections.Generic;
using TradeLink.Common;
using System.Reflection;
using TradeLink.API;

namespace TradeLink.AppKit
{
    /// <summary>
    /// track results
    /// </summary>
    public class Results : Result
    {
        /// <summary>
        /// create default results instance
        /// </summary>
        public Results() : this(.01m, .01m, 0) { }
        /// <summary>
        /// create results instance with risk free return, comission and report time
        /// </summary>
        /// <param name="rfr"></param>
        /// <param name="com"></param>
        /// <param name="reporttime">0 to disable reports, otherwise 16:46:00 = 164600</param>
        public Results(decimal rfr, decimal com, int reporttime)
        {
            RiskFreeRate = rfr;
            Comission = com;
            ReportTime = reporttime;
        }
        int _livecheckafterXticks = 1;
        /// <summary>
        /// wait to do live test after X ticks have arrived
        /// </summary>
        public int CheckLiveAfterTickCount { get { return _livecheckafterXticks; } set { _livecheckafterXticks = value; } }
        int _livetickdelaymax = 60;
        /// <summary>
        /// if a tick is within this many seconds of current system time on same day, tick stream is considered live and reports can be sent
        /// </summary>
        public int CheckLiveMaxDelaySec { get { return _livetickdelaymax; } set { _livetickdelaymax = value; } }
        decimal rfr = .01m;
        decimal RiskFreeRate { get { return rfr; } set { rfr = value; } }
        decimal com = .01m;
        decimal Comission { get { return com; } set { com = value; } }
        List<Trade> fills = new List<Trade>();
        /// <summary>
        /// pass fills as they arrive
        /// </summary>
        /// <param name="fill"></param>
        public void GotFill(Trade fill)
        {
            fills.Add(fill);
        }
        /// <summary>
        /// pass new positions as they arrive
        /// </summary>
        /// <param name="p"></param>
        public void GotPosition(Position p)
        {
            if (p.isFlat)
                return;
            fills.Add(p.ToTrade());
        }

        int _rt = 0;
        bool sendreport = false;
        bool SendReport { get { return sendreport; } set { sendreport = value; } }
        int ReportTime { get { return _rt; } set { _rt = value; sendreport = (_rt != 0); } }
        bool _livecheck = true;
        bool _islive = false;
        public bool isLive { get { return _islive; } }

        System.Text.StringBuilder _msg = new System.Text.StringBuilder(bufsize);

        /// <summary>
        /// pass debugs to results for report generation
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="appendtime"></param>
        public void GotDebug(string msg, bool appendtime)
        {
            if (appendtime)
                _msg.AppendLine(_time + ": " + msg);
            else
                _msg.AppendLine(msg);
        }
        /// <summary>
        /// pass debug messages to results for report generation
        /// </summary>
        /// <param name="msg"></param>
        public void GotDebug(string msg)
        {
            _msg.AppendLine(_time + ": " + msg);
        }
        int _time = 0;
        int _ticks = 0;
        /// <summary>
        /// pass ticks as they arrive (only necessary if using report time)
        /// </summary>
        /// <param name="k"></param>
        public void newTick(Tick k)
        {
            _time = k.time;
            if (_livecheck && (_ticks++>CheckLiveAfterTickCount))
            {
                bool dmatch = k.date == Util.ToTLDate();
                bool tmatch = Util.FTDIFF(k.time, Util.ToTLTime()) < CheckLiveMaxDelaySec;
                _islive = dmatch && tmatch;
                _livecheck = false;

            }
            ScheduledReportHit(k.time);
        }

        public bool ScheduledReportHit(int time)
        {
            if (_islive && sendreport && (time>=_rt))
            {
                sendreport = false;
                debug("hit report time: " + ReportTime + " at: " + time);
                Report();
                return true;
            }
            return false;
        }


        const int bufsize = 100000;
        /// <summary>
        /// generate current report as report event
        /// </summary>
        public void Report()
        {
            if (SendReportEvent != null)
            {
                _msg.Insert(0, FetchResults().ToString());
                SendReportEvent(_msg.ToString());
                _msg = new System.Text.StringBuilder(bufsize);
            }
        }
        void debug(string msg)
        {
            if (SendDebugEvent != null)
                SendDebugEvent(msg);
        }
        public event DebugDelegate SendDebugEvent;
        public event DebugDelegate SendReportEvent;
        public Results FetchResults() { return FetchResults(RiskFreeRate, Comission); }
        public Results FetchResults(decimal rfr, decimal commiss) { return FetchResults(TradeResult.ResultsFromTradeList(fills), rfr, commiss, debug); }
        /// <summary>
        /// get results from list of traderesults
        /// </summary>
        /// <param name="results"></param>
        /// <param name="RiskFreeRate"></param>
        /// <param name="CommissionPerContractShare"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static Results FetchResults(List<TradeResult> results, decimal RiskFreeRate, decimal CommissionPerContractShare, DebugDelegate d)
        {
            return FetchResults(results, RiskFreeRate, CommissionPerContractShare, true, d);
        }
        public static Results FetchResults(List<TradeResult> results, decimal RiskFreeRate, decimal CommissionPerContractShare, bool persymbol, DebugDelegate d)
        {
            try
            {
                List<Trade> fills = new List<Trade>();
                foreach (TradeResult tr in results)
                    fills.Add(tr.Source);
                List<decimal> _MIU = new List<decimal>();
                List<decimal> _grossreturn = new List<decimal>();
                List<decimal> _netreturns = new List<decimal>();
                List<decimal> pctrets = new List<decimal>();
                List<int> days = new List<int>();
                //clear position tracker
                PositionTracker pt = new PositionTracker(results.Count);
                // setup new results
                Results r = new Results();
                r.ResultsDateTime = Util.ToTLDateTime();
                r.ComPerShare = CommissionPerContractShare;
                int consecWinners = 0;
                int consecLosers = 0;
                List<long> exitscounted = new List<long>();
                decimal winpl = 0;
                decimal losepl = 0;
                Dictionary<string, int> tradecount = new Dictionary<string, int>();
                List<decimal> negret = new List<decimal>(results.Count);
                decimal curcommiss = 0;

                for (int i = 0; i<results.Count; i++)
                {
                    var tr = results[i];
                    if (tradecount.ContainsKey(tr.Source.symbol))
                        tradecount[tr.Source.symbol]++;
                    else
                        tradecount.Add(tr.Source.symbol, 1);
                    if (!days.Contains(tr.Source.xdate))
                        days.Add(tr.Source.xdate);
                    var pos = pt[tr.Source.symbol];
                    int usizebefore = pos.UnsignedSize;
                    decimal pricebefore = pos.AvgPrice;
                    var islongbefore = pos.isLong;
                    var miubefore = pos.isFlat ? 0 : pos.UnsignedSize * pos.AvgPrice;
                    decimal pospl = pt.Adjust(tr.Source);
                    
                    bool islong = pt[tr.Source.symbol].isLong;
                    bool isroundturn = (usizebefore != 0) && (pt[tr.Source.symbol].UnsignedSize == 0);
                    // get comissions
                    curcommiss += Math.Abs(tr.Source.xsize) * CommissionPerContractShare;
                    bool isclosing = pt[tr.Source.symbol].UnsignedSize<usizebefore;
                    // calculate MIU and store on array
                    var miu = pt[tr.Source.symbol].isFlat ? 0 : pt[tr.Source.symbol].AvgPrice * pt[tr.Source.symbol].UnsignedSize;
                    if (miu!=0)
                        _MIU.Add(miu);
                    // get miu up to this point
                    var maxmiu = _MIU.Count == 0 ? 0 : Math.Abs(Calc.Max(_MIU.ToArray()));
                    // if we closed something, update return
                    decimal grosspl = 0;
                    if (isclosing)
                        if (islongbefore)
                            grosspl = (tr.Source.xprice - pricebefore) * Math.Abs(tr.Source.xsize);
                        else
                            grosspl = (pricebefore - tr.Source.xprice) * Math.Abs(tr.Source.xsize);
                    decimal netpl = grosspl - (Math.Abs(tr.Source.xsize) * CommissionPerContractShare);
                    // get p&l for portfolio
                    curcommiss = 0;
                    // count return
                    _grossreturn.Add(grosspl);
                    _netreturns.Add(netpl);
                    // get pct return for portfolio
                    decimal pctret = 0;
                    if (miubefore == 0)
                        pctret = netpl / miu;
                    else
                        pctret = netpl / miubefore;
                    pctrets.Add(pctret);
                    // if it is below our zero, count it as negative return

                    if (pctret < 0)
                        negret.Add(pctret);
                  
                    if (isroundturn)
                    {
                        r.RoundTurns++;
                        if (pospl >= 0)
                            r.RoundWinners++;
                        else if (pospl < 0)
                            r.RoundLosers++;

                    }
                    

                    if (!r.Symbols.Contains(tr.Source.symbol))
                        r.Symbols += tr.Source.symbol + ",";
                    r.Trades++;
                    r.SharesTraded += Math.Abs(tr.Source.xsize);
                    r.GrossPL += tr.ClosedPL;

                    

                    if ((tr.ClosedPL > 0) && !exitscounted.Contains(tr.Source.id))
                    {
                        if (tr.Source.side)
                        {
                            r.SellWins++;
                            r.SellPL += tr.ClosedPL;
                        }
                        else
                        {
                            r.BuyWins++;
                            r.BuyPL += tr.ClosedPL;
                        }
                        if (tr.Source.id != 0)
                            exitscounted.Add(tr.id);
                        r.Winners++;
                        consecWinners++;
                        consecLosers = 0;
                    }
                    else if ((tr.ClosedPL < 0) && !exitscounted.Contains(tr.Source.id))
                    {
                        if (tr.Source.side)
                        {
                            r.SellLosers++;
                            r.SellPL += tr.ClosedPL;
                        }
                        else
                        {
                            r.BuyLosers++;
                            r.BuyPL += tr.ClosedPL;
                        }
                        if (tr.Source.id != 0)
                            exitscounted.Add(tr.id);
                        r.Losers++;
                        consecLosers++;
                        consecWinners = 0;
                    }
                    if (tr.ClosedPL > 0)
                        winpl += tr.ClosedPL;
                    else if (tr.ClosedPL < 0)
                        losepl += tr.ClosedPL;

                    if (consecWinners > r.ConsecWin) r.ConsecWin = consecWinners;
                    if (consecLosers > r.ConsecLose) r.ConsecLose = consecLosers;
                    if ((tr.OpenSize == 0) && (tr.ClosedPL == 0)) r.Flats++;
                    if (tr.ClosedPL > r.MaxWin) r.MaxWin = tr.ClosedPL;
                    if (tr.ClosedPL < r.MaxLoss) r.MaxLoss = tr.ClosedPL;
                    if (tr.OpenPL > r.MaxOpenWin) r.MaxOpenWin = tr.OpenPL;
                    if (tr.OpenPL < r.MaxOpenLoss) r.MaxOpenLoss = tr.OpenPL;

                }

                if (r.Trades != 0)
                {
                    r.AvgPerTrade = Math.Round((losepl + winpl) / r.Trades, 2);
                    r.AvgLoser = r.Losers == 0 ? 0 : Math.Round(losepl / r.Losers, 2);
                    r.AvgWin = r.Winners == 0 ? 0 : Math.Round(winpl / r.Winners, 2);
                    r.MoneyInUse = Math.Round(Calc.Max(_MIU.ToArray()), 2);
                    r.MaxPL = Math.Round(Calc.Max(_grossreturn.ToArray()), 2);
                    r.MinPL = Math.Round(Calc.Min(_grossreturn.ToArray()), 2);
                    r.MaxDD = Calc.MaxDDPct(fills);
                    r.SymbolCount = pt.Count;
                    r.DaysTraded = days.Count;
                    r.GrossPerDay = Math.Round(r.GrossPL / days.Count, 2);
                    r.GrossPerSymbol = Math.Round(r.GrossPL / pt.Count, 2);
                    if (persymbol)
                    {
                        for (int i = 0; i < pt.Count; i++)
                        {
                            r.PerSymbolStats.Add(pt[i].symbol + ": " + tradecount[pt[i].symbol] + " for " + pt[i].ClosedPL.ToString("C2"));
                        }
                    }
                }
                else
                {
                    r.MoneyInUse = 0;
                    r.MaxPL = 0;
                    r.MinPL = 0;
                    r.MaxDD = 0;
                    r.GrossPerDay = 0;
                    r.GrossPerSymbol = 0;
                }

                r.PctReturns = pctrets.ToArray();
                r.DollarReturns = _netreturns.ToArray();
                r.NegPctReturns = negret.ToArray();

                
                try
                {
                    var fret = Calc.Avg(pctrets.ToArray());
                    
                    if (pctrets.Count == 0)
                        r.SharpeRatio = 0;
                    else if (pctrets.Count == 1)
                        r.SharpeRatio = Math.Round((fret- RiskFreeRate), 3);
                    else
                        r.SharpeRatio = Math.Round(Calc.SharpeRatio(fret, Calc.StdDev(pctrets.ToArray()), RiskFreeRate), 3);
                }
                catch (Exception ex)
                {
                    if (d != null)
                        d("sharpe error: " + ex.Message);
                }

                try
                {
                    var fret = Calc.Avg(pctrets.ToArray());
                    if (pctrets.Count == 0)
                        r.SortinoRatio = 0;
                    else if (negret.Count == 1)
                        r.SortinoRatio = (fret - RiskFreeRate) / negret[0];
                    else if ((negret.Count == 0))
                        r.SortinoRatio = fret - RiskFreeRate;
                    else
                        r.SortinoRatio = Math.Round(Calc.SortinoRatio(fret, Calc.StdDev(negret.ToArray()), RiskFreeRate ), 3);
                }
                catch (Exception ex)
                {
                    if (d != null)
                        d("sortino error: " + ex.Message);
                }


                

                return r;
            }
            catch (Exception ex)
            {
                if (d != null)
                    d("error generting report: " + ex.Message + ex.StackTrace);
                return new Results();
            }

        }

        string _resultid = string.Empty;
        public string ResultsId { get { if (string.IsNullOrWhiteSpace(_resultid)) return ResultsDateTime + "/" + NetPL_Nice + "/" + SimParameters; return _resultid; } set { _resultid = value; } }

        bool _persymbol = true;
        bool ShowPerSymbolStats { get { return _persymbol; } set { _persymbol = value; } }

        public static Results ResultsFromTradeList(List<Trade> trades)
        {
            return ResultsFromTradeList(trades, .01m, .01m, null);
        }
        public static Results ResultsFromTradeList(List<Trade> trades, DebugDelegate d)
        {
            return ResultsFromTradeList(trades, .01m, .01m, d);
        }

        /// <summary>
        /// get results from list of trades
        /// </summary>
        /// <param name="trades"></param>
        /// <param name="riskfreerate"></param>
        /// <param name="commissionpershare"></param>
        /// <param name="d"></param>
        /// <returns></returns>
        public static Results ResultsFromTradeList(List<Trade> trades, decimal riskfreerate, decimal commissionpershare, DebugDelegate d)
        {
            string[] results = Util.TradesToClosedPL(trades);
            List<TradeResult> tresults = new List<TradeResult>(results.Length);
            foreach (string line in results)
                tresults.Add(TradeResult.Init(line));
            return Results.FetchResults(tresults, riskfreerate, commissionpershare, d);
        }

        List<string> _persym = new List<string>();
        public List<string> PerSymbolStats { get { return _persym; } set { _persym = value; } }

        public long ResultsDateTime { get; set; }
        string _syms = string.Empty;
        public string Symbols { get { return _syms; } set { _syms = value; } }
        public decimal GrossPL { get; set; }
        public string NetPL_Nice { get { return v2s(NetPL); } }
        public decimal NetPL { get { return GrossPL - (SharesTraded*ComPerShare); } }
        public decimal BuyPL { get; set; }
        public decimal SellPL { get; set; }
        public int Winners { get; set; }
        public int BuyWins { get; set; }
        public int SellWins { get; set; }
        public int SellLosers { get; set; }
        public int BuyLosers { get; set; }
        public int Losers { get; set; }
        public int Flats { get; set; }
        public decimal AvgPerTrade { get; set; }
        public decimal AvgWin { get; set; }
        public decimal AvgLoser { get; set; }
        public decimal MoneyInUse { get; set; }
        public decimal MaxPL { get; set; }
        public decimal MinPL { get; set; }
        public decimal MaxDD { get; set; }
        public string MaxDD_Nice { get { return MaxDD.ToString("P2"); } }
        public decimal MaxWin { get; set; }
        public decimal MaxLoss { get; set; }
        public decimal MaxOpenWin { get; set; }
        public decimal MaxOpenLoss { get; set; }
        public int SharesTraded { get; set; }
        public int RoundTurns { get; set; }
        public int RoundWinners { get; set; }
        public int RoundLosers { get; set; }
        public int HundredLots { get { return (int)Math.Round((double)SharesTraded / 100, 0); } }
        public int Trades { get; set; }
        public int SymbolCount { get; set; }
        public int DaysTraded { get; set; }
        public decimal GrossPerDay { get; set; }
        public decimal GrossPerSymbol { get; set; }
        public decimal SharpeRatio { get; set; }
        public decimal SortinoRatio { get; set; }
        public decimal[] DollarReturns { get; set; }
        public decimal[] PctReturns { get; set; }
        public decimal[] NegPctReturns { get; set; }
        private decimal _compershare = 0.01m;
        public decimal ComPerShare { get { return _compershare; } set { _compershare = value; } }
        public int ConsecWin { get; set; }
        public int ConsecLose { get; set; }
        string _simparam = string.Empty;
        public string SimParameters { get { return _simparam; } set { _simparam = value; } }
        public string WinSeqProbEffHyp { get { return v2s(Math.Min(100, ((decimal)Math.Pow(1 / 2.0, ConsecWin) * (Trades - Flats - ConsecWin + 1)) * 100)) + @" %"; } }
        public string LoseSeqProbEffHyp { get { return v2s(Math.Min(100, ((decimal)Math.Pow(1 / 2.0, ConsecLose) * (Trades - Flats - ConsecLose + 1)) * 100)) + @" %"; } }
        public string Commissions { get { return v2s(HundredLots * 100 * ComPerShare); } }
        string v2s(decimal v) { return v.ToString("N2"); }
        public string WLRatio { get { return v2s((Losers == 0) ? 0 : (Winners / Losers)); } }
        public string GrossMargin { get { return v2s((GrossPL == 0) ? 0 : ((GrossPL - (HundredLots * 100 * ComPerShare)) / GrossPL)); } }
        public string RiskFreeRet { get { return RiskFreeRate.ToString("P2"); } }

        /// <summary>
        /// get string version of results table
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(":\t");
        }
        /// <summary>
        /// get results like calc => value where '=>' is the delim
        /// </summary>
        /// <param name="delim"></param>
        /// <returns></returns>
        public string ToString(string delim)
        {
            return ToString(delim, true);
        }
        public string ToString(string delim, bool genpersymbol)
        {
            Type t = GetType();
            FieldInfo[] fis = t.GetFields();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (FieldInfo fi in fis)
            {
                string format = null;
                if (fi.FieldType == typeof(Decimal)) format = "{0:N2}";
                if (fi.Name == "PerSymbolStats")
                    continue;
                if (fi.FieldType == typeof(decimal[]))
                {
                    var array = (decimal[])fi.GetValue(this);
                    sb.AppendLine(fi.Name + delim+Util.join(array));
                    continue;
                }
                sb.AppendLine(fi.Name + delim + (format != null ? string.Format(format, fi.GetValue(this)) : fi.GetValue(this).ToString()));
            }
            PropertyInfo[] pis = t.GetProperties();
            foreach (PropertyInfo pi in pis)
            {
                string format = null;
                if (pi.Name == "PerSymbolStats")
                    continue;
                if (pi.PropertyType == typeof(Decimal)) format = "{0:N2}";
                if (pi.PropertyType== typeof(decimal[]))
                {
                    var array = (decimal[])pi.GetValue(this,null);
                    sb.AppendLine(pi.Name + delim+Util.join(array));
                    continue;
                }
                sb.AppendLine(pi.Name + delim + (format != null ? string.Format(format, pi.GetValue(this, null)) : pi.GetValue(this, null).ToString()));
            }
            foreach (string ps in PerSymbolStats)
            {
                if ((ps == null) || (ps == string.Empty))
                    continue;

                string pst = ps.Replace(":", delim);
                sb.AppendLine(pst);
            }
            return sb.ToString();

        }
    }

}
