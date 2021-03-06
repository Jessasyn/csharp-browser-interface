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
    /// Note that running these tests: <br/>
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

            Assert.ThrowsException<FormatException>(() => browserHandler.OpenUrl(string.Empty));

        }
        /// <summary>
        /// Tests whether a <see cref="BrowserHandler"/> can open 10 urls after one another.
        /// </summary>
        [TestMethod]
        [TestCategory("Positive")]
        public void TestUrls()
        {
            using BrowserHandler browserHandler = new BrowserHandler();

            for (int i = 0; i < 10; i++)
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

            Assert.ThrowsException<ObjectDisposedException>(() => browserHandler.OpenUrl("http://www.google.com"));
        }

        /// <summary>
        /// Tests whether a <see cref="BrowserHandler"/> throws a <see cref="FormatException"/> when it is provided with a string which is not an HTTP <see cref="Uri"/>.
        /// </summary>
        [TestMethod]
        [TestCategory("Negative")]
        public void TestFTPUrl()
        {
            using BrowserHandler browserHandler = new BrowserHandler();

            Assert.ThrowsException<ArgumentException>(() => browserHandler.OpenUrl("ftp://www.google.com"));
        }

        /// <summary>
        /// Tests whether a <see cref="BrowserHandler"/> throws a <see cref="InvalidOperationException"/> when it is provided with a string which is not a <see cref="Uri"/>.
        /// </summary>
        [TestMethod]
        [TestCategory("Negative")]
        public void TestInvalidUrl()
        {
            using BrowserHandler browserHandler = new BrowserHandler();

            Assert.ThrowsException<FormatException>(() => browserHandler.OpenUrl(_random?.NextString(15) ?? string.Empty));
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
        [TestMethod]
        [TestCategory("Negative")]
        public void TestKeyCollission()
        {
            using BrowserHandler browserHandler = new BrowserHandler();

            Assert.ThrowsException<InvalidOperationException>(() => browserHandler.OpenUrl("https://www.google.com", new Dictionary<object, object> { { "3", "3" }, { 3, "3" } }));
        }
    }

    /// <summary>
    /// This class contains several extensions to make generating random urls of varying length easier.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Generates a random string of <paramref name="len"/> characters, where each character is uppercase, lowercase or a digit.
        /// </summary>
        /// <param name="random">The instance of <see cref="Random"/> which is used to generate a random number between 0 and 26.</param>
        /// <param name="len">The length the string has to be.</param>
        /// <returns>A <see cref="string"/>, of length <paramref name="len"/>, composed of random letters or digits.</returns>
        internal static string NextString(this Random random, int len)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int i = 0; i < len; i++)
            {
                stringBuilder.Append(Convert.ToChar(random.Next(0, 26) + 65));
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Generates a valid http url, which has <paramref name="segmentCount"/> random segments of length <paramref name="segmentLen"/>, separated by <paramref name="separator"/>.
        /// </summary>
        /// <param name="random">The instance of <see cref="Random"/> which is used to generate a random number between 0 and 26.</param>
        /// <param name="segmentCount">The number of segments the url has to have.</param>
        /// <param name="segmentLen">The length of each segment.</param>
        /// <param name="trimEnd">Whether to remove the trailing separator or not.</param>
        /// <param name="separator">The character to use to separate segments.</param>
        /// <returns>A <see cref="string"/> containing the random url.</returns>
        internal static string NextUrl(this Random random, int segmentCount, int segmentLen, bool trimEnd = false, char separator = '/')
        {
            StringBuilder stringBuilder = new StringBuilder($"http{(random.Next(1, 101) % 2 == 0 ? "s" : string.Empty)}://www.");

            for (int i = 0; i < segmentCount; i++)
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
