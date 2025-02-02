using System;
using TradeLink.Common;
using NUnit.Framework;

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
    public class TestAccount
    {
        public TestAccount() { }

        [Test]
        public void Basics()
        {
            Account a = new Account();
            Assert.That(!a.isValid);
            const string myid = "jfranta";
            a = new Account(myid);
            Assert.That(a.isValid);
            Assert.That(a.ID == myid);
        }

    }
}
