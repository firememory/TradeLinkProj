using System;
using System.Collections.Generic;
using System.Text;
using TradeLink.API;
using System.IO;
using System.Reflection;

namespace TradeLink.Common
{
    /// <summary>
    /// used for loading responses from disk (via DLL) or from already loaded assemblies.
    /// </summary>
    public static class ResponseLoader
    {

        public static bool IsResponse(Type t)
        {
            return typeof(Response).IsAssignableFrom(t);
        }

        /// <summary>
        /// Create a single Response from a DLL containing many Responses.  
        /// </summary>
        /// <param name="fullname"></param>
        /// <param name="dllname"></param>
        /// <param name="deb"></param>
        /// <returns></returns>
        public static Response FromDLL(string fullname, string dllname, DebugDelegate deb)
        {
            try
            {
                return FromDLL(fullname, dllname);
            }
            catch (Exception ex)
            {
                if (deb != null)
                {
                    deb(ex.Message + ex.StackTrace);

                }
            }
            return null;
        }
        /// <summary>
        /// Create a single Response from a DLL containing many Responses.  
        /// </summary>
        /// <param name="fullname">The fully-qualified Response Name (as in 'BoxExamples.Name').  </param>
        /// <param name="dllname">The path and filename of DLL.</param>
        /// <returns></returns>
        public static Response FromDLL(string fullname, string dllname)
        {
            System.Reflection.Assembly a;
            
#if (DEBUG)
            a = System.Reflection.Assembly.LoadFrom(dllname);
#else
            byte[] raw = loadFile(dllname);
            a = System.Reflection.Assembly.Load(raw);
#endif
            return FromAssembly(a, fullname);
        }
        /// <summary>
        /// Create a single Response from an Assembly containing many Responses. 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="fullname"></param>
        /// <param name="deb"></param>
        /// <returns></returns>
        public static Response FromAssembly(System.Reflection.Assembly a, string fullname) { return FromAssembly(a, fullname, null); }

        /// <summary>
        /// Create a single Response from an Assembly containing many Responses. 
        /// </summary>
        /// <param name="a">the assembly object</param>
        /// <param name="boxname">The fully-qualified Response Name (as in Response.FullName).</param>
        /// <returns></returns>
        public static Response FromAssembly(System.Reflection.Assembly a, string fullname, DebugDelegate deb)
        {
            Response b = null;
            try
            {

                Type type;
                object[] args;
                
                // get class from assembly
                type = a.GetType(fullname, true, true);
                args = new object[] { };
                // create an instance of type and cast to response
                b = (Response)Activator.CreateInstance(type, args);
                // if it doesn't have a name, add one
                if (b.Name == string.Empty)
                {
                    b.Name = type.Name;
                }
                if (b.FullName == string.Empty)
                {
                    b.FullName = type.FullName;
                }
                return b;
            }
            catch (Exception ex)
            {
                if (deb != null)
                {
                    deb(ex.Message + ex.StackTrace);
                }
                b = new InvalidResponse();
                b.Name = ex.Message + ex.StackTrace;
                return b;
            }
        }

        static byte[] loadFile(string filename)
        {
            // get file
            System.IO.FileStream fs = new System.IO.FileStream(filename, System.IO.FileMode.Open,  System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite);
            // prepare buffer based on file size
            byte[] buffer = new byte[(int)fs.Length];
            // read file into buffer
            fs.Read(buffer, 0, buffer.Length);
            // close file
            fs.Close();
            // return buffer
            return buffer;

        }


        /// <summary>
        /// Gets full Response names found in a given file.
        /// </summary>
        /// <param name="boxdll">The file path of the assembly containing the boxes.</param>
        /// <returns></returns>
        public static List<string> GetResponseList(string dllfilepath) { return GetResponseList(dllfilepath, null); }
        public static List<string> GetResponseList(string dllfilepath, DebugDelegate d)
        {
            List<string> reslist = new List<string>();
            if (!File.Exists(dllfilepath)) return reslist;
            Assembly a;
            try
            {
                a = Assembly.LoadFile(dllfilepath);
            }
            catch (Exception ex) { reslist.Add(ex.Message); return reslist; }
            return GetResponseList(a, d);
        }

        /// <summary>
        /// Gets all Response names found in a given assembly.  Names are FullNames, which means namespace.FullName.  eg 'BoxExamples.BigTradeUI'
        /// </summary>
        /// <param name="boxdll">The assembly.</param>
        /// <returns>list of response names</returns>
        public static List<string> GetResponseList(Assembly responseassembly) { return GetResponseList(responseassembly, null); }
        public static List<string> GetResponseList(Assembly responseassembly, DebugDelegate deb)
        {
            List<string> reslist = new List<string>();
            Type[] t;
            try
            {
                t = responseassembly.GetTypes();
                for (int i = 0; i < t.GetLength(0); i++)
                    if (IsResponse(t[i])) reslist.Add(t[i].FullName);
            }
            catch (Exception ex)
            {
                if (deb != null)
                {
                    deb(ex.Message + ex.StackTrace);
                }
            }

            return reslist;
        }



        public const MessageTypes SimHintDayMsg = MessageTypes.SENDDATEHINT;
        public const MessageTypes SimHintTimeMsg = MessageTypes.SENDTIMEHINT;
        public const MessageTypes SimHintSymbolsMsg = MessageTypes.SENDSYMBOLHINT;

        public static string[] GetResetSymbols(string dllpath, string fullname) { return GetResetSymbols(dllpath, fullname, false); }
        public static string[] GetResetSymbols(string dllpath, string fullname, bool fullsyms)
        {
            System.Reflection.Assembly a;

#if (DEBUG)
            a = System.Reflection.Assembly.LoadFrom(dllpath);
#else
            byte[] raw = loadFile(dllpath);
            a = System.Reflection.Assembly.Load(raw);
#endif
            return GetResetSymbols(a, fullname, fullsyms);
        }
        public static string[] GetResetSymbols(Assembly asm, string fullname) { return GetResetSymbols(asm, fullname, false); }
        public static string[] GetResetSymbols(Assembly asm, string fullname, bool fullsyms)
        {
            var r = FromAssembly(asm, fullname);
            if ((r == null) || !r.isValid)
                return new string[0];
            bindtmp(r);
            try
            {
                r.Reset();
            }
            catch { }
            return fullsyms ? tmp_basket.ToSymArrayFull() : tmp_basket.ToSymArray();
        }

        public static BarRequest[] GetExpBarRequests(string dllpath, string fullname, DebugDelegate d)
        {
#if (DEBUG)
            var a = System.Reflection.Assembly.LoadFrom(dllpath);
#else
            byte[] raw = loadFile(dllpath);
            var a = System.Reflection.Assembly.Load(raw);
#endif
            return GetExpBarRequests(a, fullname,d);
        }
        public static BarRequest[] GetExpBarRequests(Assembly asm, string fullname, DebugDelegate d)
        {
            var r = FromAssembly(asm, fullname);
            if ((r == null) || !r.isValid)
            {
                if (d != null)
                {
                    d("error loading design: " + fullname + " from: " + asm.FullName + " err: " + r.Name + " " + r.FullName);
                }

                return new BarRequest[0];
            }
            // send fake hints
            SendSimulationHints(ref r, Util.ToTLDate(), Util.ToTLTime(), new string[] { "TST" }, null);
            bindtmp(r);
            try
            {

                r.Reset();
            }
            catch { }

            return tmp_barreqs.ToArray();
        }

        public static bool SendSimulationHints(ref Response r) { return SendSimulationHints(ref r, Util.ToTLDate(), Util.ToTLTime(), new string[0], null); }
        public static bool SendSimulationHints(ref Response r, DebugDelegate deb) { return SendSimulationHints(ref r, Util.ToTLDate(), Util.ToTLTime(), new string[0], deb); }
        public static bool SendSimulationHints(ref Response r, int date, DebugDelegate deb) { return SendSimulationHints(ref r, date, 0, new string[0], deb); }
        public static bool SendSimulationHints(ref Response r, int date, int time, DebugDelegate deb) { return SendSimulationHints(ref r, date, time, new string[0], deb); }
        public static bool SendSimulationHints(ref Response r, int date, int time, string[] syms, DebugDelegate deb)
        {
            debs = deb;
            // prep data
            var shsyms = Util.cjoin(syms);
            var shdate = date.ToString();
            var shtime = time.ToString();
            string response = string.Empty;
            bool ok = true;
            // send
            try
            {
                r.GotMessage(SimHintDayMsg, 0, 0, 0, shdate, ref response);
            }
            catch (Exception ex) { ok = false; debug("error sending response message: " + SimHintDayMsg.ToString() + ", err: " + ex.Message + ex.StackTrace); }
            try
            {
                r.GotMessage(SimHintSymbolsMsg, 0, 0, 0, shsyms, ref response);
            }
            catch (Exception ex) { ok = false; debug("error sending response message: " + SimHintSymbolsMsg.ToString() + ", err: " + ex.Message + ex.StackTrace); }
            try
            {
                r.GotMessage(SimHintTimeMsg, 0, 0, 0, shtime, ref response);
            }
            catch (Exception ex) { ok = false; debug("error sending response message: " + SimHintTimeMsg.ToString() + ", err: " + ex.Message + ex.StackTrace); }

            debug("sent simhints: " + shdate + "/" + shtime + " " + shsyms);

            return ok;

        }

        public static string[] GetIndicators(string dll, string responsename, DebugDelegate d)
        {
            var r = ResponseLoader.FromDLL(responsename, dll, d);
            if ((r == null) || !r.isValid)
                return new string[0];
            bindtmp(r);
            try
            {
                r.Reset();
            }
            catch { }
            return r.Indicators;
        }

        public static List<string> GetSettableList(string dll, string responsename, DebugDelegate d)
        {
            var r = ResponseLoader.FromDLL(responsename, dll, d);
            return GetSettableList(r);
        }
        public static List<string> GetSettableList(Response rtmp)
        {
            List<string> valnames = new List<string>();
            if (rtmp == null)
                return valnames;
            if (!rtmp.isValid)
                return valnames;
            bindtmp(rtmp);
            try
            {
                rtmp.Reset();
            }
            catch { }
            rtmp.SendMessageEvent -= new MessageDelegate(settable_SendMessageEvent);
            if (string.IsNullOrWhiteSpace(tmp_settablelist))
                return valnames;
            valnames = new List<string>(tmp_settablelist.Split(','));
            return valnames;

        }

        static void resettmpvars()
        {
            tmp_settablelist = string.Empty;
            //tmp_basketid = -1;
            tmp_basket = new BasketImpl();
            tmp_barreqs.Clear();

        }

        static void bindtmp(Response rtmp)
        {
            rtmp.ID = 0;
            resettmpvars();
            rtmp.SendMessageEvent += new MessageDelegate(settable_SendMessageEvent);
            rtmp.SendTicketEvent += new TicketDelegate(r_SendTicketEvent);
            rtmp.SendCancelEvent += new LongSourceDelegate(r_SendCancelEvent);
            rtmp.SendBasketEvent += new BasketDelegate(r_SendBasketEvent);
            rtmp.SendChartLabelEvent += new ChartLabelDelegate(r_SendChartLabelEvent);
            rtmp.SendDebugEvent += new DebugDelegate(r_SendDebugEvent);
            rtmp.SendIndicatorsEvent += new ResponseStringDel(r_SendIndicatorsEvent);
            rtmp.SendMessageEvent += new MessageDelegate(r_SendMessageEvent);
            rtmp.SendOrderEvent += new OrderSourceDelegate(r_SendOrderEvent);
        }

        static List<BarRequest> tmp_barreqs = new List<BarRequest>();

        static string tmp_settablelist = string.Empty;
        static void settable_SendMessageEvent(MessageTypes type, long source, long dest, long msgid, string request, ref string response)
        {
            switch (type)
            {
                case MessageTypes.SENDUSERSETTABLE:
                    tmp_settablelist = request;
                    break;
                case MessageTypes.BARREQUEST:
                    {
                        var br = BarRequest.Deserialize(request);
                        if (br.isValid)
                        {
                            br.symbol = string.Empty;
                            bool addit = true;
                            for (int i = 0; i < tmp_barreqs.Count; i++)
                            {
                                var cmp = tmp_barreqs[i];
                                if ((cmp.CustomInterval == br.CustomInterval) && (cmp.Interval == br.Interval) && (cmp.BarsBackExplicit == br.BarsBackExplicit))
                                {
                                    addit = false;
                                    break;
                                }
                            }
                            if (addit)
                                tmp_barreqs.Add(br);
                        }
                    }
                    break;

            }
        }


        static void r_SendOrderEvent(Order o, int source)
        {

        }

        static void r_SendMessageEvent(MessageTypes type, long source, long dest, long msgid, string request, ref string response)
        {

        }

        static void r_SendIndicatorsEvent(int idx, string data)
        {

        }

        static void r_SendDebugEvent(string msg)
        {

        }

        static void r_SendChartLabelEvent(decimal price, int time, string label, System.Drawing.Color c)
        {

        }

        static Basket tmp_basket = new BasketImpl();
        //static int tmp_basketid = -1;
        static void r_SendBasketEvent(Basket b, int id)
        {
            tmp_basket = b;
          //  tmp_basketid = -1;
        }

        static void r_SendCancelEvent(long val, int source)
        {

        }

        static void r_SendTicketEvent(string space, string user, string password, string summary, string description, Priority pri, TicketStatus stat)
        {

        }

        static DebugDelegate debs = null;
        public static void debug(string msg)
        {
            if (debs != null)
                debs(msg);
#if DEBUG
            else
                Console.WriteLine(msg);
#endif
        }

    }

}
