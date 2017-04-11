// Created by Sebastian Mueller (smueller@ifi.uzh.ch) from the University of Zurich
// Created: 2017-04-11
// 
// Licensed under the MIT License.

using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace FitbitTracker.Tests
{
    [TestClass()]
    public class SecretStorageTests
    {
        [TestMethod()]
        public void ReplaceDummyFitbitCredentialsTest()
        {
            SecretStorage.SaveFitbitClientID("a");
            SecretStorage.SaveFitbitClientSecret("b");
            SecretStorage.SaveFitbitFirstAuthorizationCode("c");
        }
    }
}