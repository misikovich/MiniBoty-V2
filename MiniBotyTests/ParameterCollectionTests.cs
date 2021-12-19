using Microsoft.VisualStudio.TestTools.UnitTesting;
using MiniBoty;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiniBoty.Tests
{
    [TestClass()]
    public class ParameterCollectionTests
    {
        [TestMethod()]
        public void GetValueTest()
        {
            var parameterCollection = new ParameterCollection();
            parameterCollection.AddParameter(new TagParam());
            parameterCollection.AddParameter(new IsActiveParam());
            parameterCollection.AddParameter(new DefaultPasteTimeoutParam());

            var tagType = parameterCollection.GetValue(ParameterType.DefaultPasteTimeoutParam);
            var tagStr = parameterCollection.GetValue("tag");
            Assert.IsNotNull(tagType);
            Assert.IsNotNull(tagStr);

        }

        [TestMethod()]
        public void SetValueTest()
        {
            var parameterCollection = new ParameterCollection();
            parameterCollection.AddParameter(new TagParam());
            parameterCollection.AddParameter(new IsActiveParam());
            parameterCollection.AddParameter(new DefaultPasteTimeoutParam());

            var tagType = parameterCollection.GetValue(ParameterType.DefaultPasteTimeoutParam);
            var tagStr = parameterCollection.GetValue("tag");

            var ok_int_to_timespan = parameterCollection.SetValue("DEF_PASTE_TIMEOUT", 25);
            var okAll = parameterCollection.SetValue("DEF_PASTE_TIMEOUT", TimeSpan.FromSeconds(40));

            var str_should_timespan = parameterCollection.SetValue("def-paste-timeout", "bebra");
            var bool_should_timespan = parameterCollection.SetValue("def-paste-timeout", true);

            var notFound = parameterCollection.SetValue("qwe", 21);
            var someDataIsNull = parameterCollection.SetValue(null, 10);

            Assert.IsTrue(ok_int_to_timespan.Succesfull);
            Assert.IsNotNull(ok_int_to_timespan.FeedbackMessage);
            Assert.AreNotEqual(ok_int_to_timespan.PreviousValue, ok_int_to_timespan.NewValue);
            var feedbackStr1 = ok_int_to_timespan.FeedbackMessage;

            Assert.IsTrue(okAll.Succesfull);
            Assert.AreNotEqual(okAll.NewValue, okAll.PreviousValue);
            var feedbackStr2 = okAll.FeedbackMessage;

            Assert.IsFalse(str_should_timespan.Succesfull);
            var feedbackStr3 = str_should_timespan.FeedbackMessage;

            Assert.IsFalse(bool_should_timespan.Succesfull);
            var feedbackStr4 = bool_should_timespan.FeedbackMessage;

            Assert.IsFalse(notFound.Succesfull);
            var feedbackStr5 = notFound.FeedbackMessage;

            Assert.IsFalse(someDataIsNull.Succesfull);
            var feedbackStr6 = someDataIsNull.FeedbackMessage;
        }

        [TestMethod()]
        public void AddParameterTest()
        {
            var parameterCollection = new ParameterCollection();
            parameterCollection.AddParameter(new TagParam());
            parameterCollection.AddParameter(new TagParam());
            parameterCollection.AddParameter(new TagParam());
            parameterCollection.AddParameter(new IsActiveParam());
            parameterCollection.AddParameter(new DefaultPasteTimeoutParam());
            parameterCollection.AddParameter(new DefaultPasteTimeoutParam());

            Assert.AreEqual(parameterCollection.Count, 3);
        }
    }
}