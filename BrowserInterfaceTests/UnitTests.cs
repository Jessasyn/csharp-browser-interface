#if DEBUG
#region MicrosoftNameSpaces
using Microsoft.VisualStudio.TestTools.UnitTesting;
#endregion MicrosoftNameSpaces

#region GenericNameSpaces
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System.Text;
using System;
#endregion GenericNameSpaces

namespace BrowserInterface.Tests
{
    /// <summary>
    /// Contains all unit tests which verify that the <see cref="BrowserHandler"/> works as intended. <br/>
    /// Note that running these tests:
    /// - will (likely) result in a large number of tabs opening on your browser. <br/>
    /// - in a CI environment will (likely) fail. <br/>
    /// - on a single machine only verifies the tests for that platform. <br/>
    /// - does not prove the tabs open correctly, because there is no way for a unit test to verify this.
    /// </summary>
    [TestClass]
    public class UnitTests
    {
        private static Random? _random;

        /// <summary>
        /// Prepares for the tests by creating a <see cref="_random"/> instance.
        /// </summary>
        [TestInitialize]
        [MemberNotNull(nameof(_random))]
        public void TestSetup() => _random = new Random();

        /// <summary>
        /// Destroys the <see cref="_random"/> instance.
        /// </summary>
        [TestCleanup]
        public void TestCleanUp() => _random = null;

        /// <summary>
        /// Tests if it works to open a single url using a browserhandler. <br/>
        /// The url will be of the format '.............../.............../.............../'.
        /// </summary>
        [TestMethod]
        [TestCategory("Positive")]
        public void TestSingleUrl()
        {
            using BrowserHandler browserHandler = new BrowserHandler();

            Assert.IsTrue(browserHandler.OpenUrl(_random?.NextUrl(3, 15) ?? string.Empty));
        }

        /// <summary>
        /// Tests whether a <see cref="BrowserHandler"/> can open a very long url.
        /// </summary>
        [TestMethod]
        [TestCategory("Positive")]
        public void TestLongUrl()
        {
            using BrowserHandler browserHandler = new BrowserHandler();

            Assert.IsTrue(browserHandler.OpenUrl(_random?.NextUrl(10, 20) ?? string.Empty));
        }

        /// <summary>
        /// Tests whether a <see cref="BrowserHandler"/> will throw a <see cref="FormatException"/> when provided with an empty url as input.
        /// </summary>
        [TestMethod]
        [TestCategory("Negative")]
        public void TestEmptyUrl()
        {
            using BrowserHandler browserHandler = new BrowserHandler();

            try
            {
                browserHandler.OpenUrl(string.Empty);
            }
            catch (FormatException)
            {
                return;
            }

            Assert.Fail($"{nameof(browserHandler)} should have thrown {nameof(FormatException)} with an empty string as input!");

        }
        /// <summary>
        /// Tests whether a <see cref="BrowserHandler"/> can open 10 urls after one another.
        /// </summary>
        [TestMethod]
        [TestCategory("Positive")]
        public void TestUrls()
        {
            using BrowserHandler browserHandler = new BrowserHandler();

            for(int i = 0; i < 10; i++)
            {
                Assert.IsTrue(browserHandler.OpenUrl(_random?.NextUrl(3, 15) ?? string.Empty));
            }
        }

        /// <summary>
        /// Tests whether a <see cref="BrowserHandler"/> throws <see cref="ObjectDisposedException"/> when it is used after being disposed.
        /// </summary>
        [TestMethod]
        [TestCategory("Negative")]
        public void TestDisposal()
        {
            using BrowserHandler browserHandler = new BrowserHandler();

            browserHandler.Dispose();

            try
            {
                browserHandler.OpenUrl("http://www.google.com");
            }
            catch (ObjectDisposedException)
            {
                return;
            }

            Assert.Fail($"Disposed {nameof(browserHandler)} should have thrown a {nameof(ObjectDisposedException)}!");
        }

        /// <summary>
        /// Tests whether a <see cref="BrowserHandler"/> throws a <see cref="FormatException"/> when it is provided with a string which is not an HTTP <see cref="Uri"/>.
        /// </summary>
        [TestMethod]
        [TestCategory("Negative")]
        public void TestFTPUrl()
        {
            using BrowserHandler browserHandler = new BrowserHandler();

            try
            {
                browserHandler.OpenUrl("ftp://www.google.com");
            }
            catch (ArgumentException)
            {
                return;
            }

            Assert.Fail($"{nameof(browserHandler)} should have thrown a {nameof(ArgumentException)} upon recieving an FTP url!");
        }

        /// <summary>
        /// Tests whether a <see cref="BrowserHandler"/> throws a <see cref="InvalidOperationException"/> when it is provided with a string which is not a <see cref="Uri"/>.
        /// </summary>
        [TestMethod]
        [TestCategory("Negative")]
        public void TestInvalidUrl()
        {
            using BrowserHandler browserHandler = new BrowserHandler();

            try
            {
                browserHandler.OpenUrl(_random?.NextString(15) ?? string.Empty);
            }
            catch (FormatException)
            {
                return;
            }

            Assert.Fail($"{nameof(browserHandler)} should have thrown a {nameof(FormatException)} upon recieving an invalid url!");
        }

        /// <summary>
        /// Tests whether a <see cref="BrowserHandler"/> can handle query parameters while opening a url.
        /// </summary>
        [TestMethod]
        [TestCategory("Positive")]
        public void TestQueryParameters()
        {
            using BrowserHandler browserHandler = new BrowserHandler();

            Assert.IsTrue(browserHandler.OpenUrl("https://www.google.com", new Dictionary<object, object> { { 3, "3" } }));
        }

        /// <summary>
        /// Tests whether a <see cref="BrowserHandler"/> which recieves colliding query key values throws a <see cref="InvalidOperationException"/>.
        /// </summary>
        /// <exception cref="Exception"></exception>
        [TestMethod]
        [TestCategory("Negative")]
        public void TestKeyCollission()
        {
            using BrowserHandler browserHandler = new BrowserHandler();

            try
            {
                browserHandler.OpenUrl("https://www.google.com", new Dictionary<object, object> { { "3", "3" }, { 3, "3" } });
            }
            catch (InvalidOperationException)
            {
                return;
            }

            Assert.Fail($"{nameof(browserHandler)} should have thrown an {nameof(InvalidOperationException)} upon detecting key colission!");
        }
    }

    /// <summary>
    /// This class contains several extensions to make generating random urls of varying length easier.
    /// </summary>
    internal static class StringExtensions
    {
        internal static string NextString(this Random random, int len)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < len; i++)
            {
                stringBuilder.Append(Convert.ToChar(random.Next(0, 26) + 65));
            }

            return stringBuilder.ToString();
        }

        internal static string NextUrl(this Random random, int segmentCount, int segmentLen, bool trimEnd = false, char separator = '/')
        {
            StringBuilder stringBuilder = new StringBuilder("http://www.");

            for(int i = 0; i < segmentCount; i++)
            {
                stringBuilder.Append(random.NextString(segmentLen));
                stringBuilder.Append(separator);
            }

            if (trimEnd)
            {
                stringBuilder.Length--;
            }
            
            return stringBuilder.ToString();
        }
    }
}
#endif