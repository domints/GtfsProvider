using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GtfsProvider.Common.Extensions;
using NUnit.Framework;

namespace GtfsProvider.Tests
{
    public class DateExtensionTests
    {
        [Test]
        [TestCase(20240803, 2024, 08, 03)]
        [TestCase(00010203, 1, 2, 3)]
        public void DoesDateDeserialize(int testCase, int year, int month, int day)
        {
            var result = testCase.ToDateOnly();
            Assert.AreEqual(year, result.Year);
            Assert.AreEqual(month, result.Month);
            Assert.AreEqual(day, result.Day);
        }

        [Test]
        [TestCase(2024, 08, 03, 20240803)]
        [TestCase(1, 2, 3, 00010203)]
        public void DoesDateSerialize(int year, int month, int day, int result)
        {
            var testCase = new DateOnly(year, month, day);

            var testResult = testCase.ToInt();

            Assert.AreEqual(result, testResult);
        }

        [Test]
        [TestCase(2024, 01, 01, 2024, 01, 02)]
        [TestCase(2024, 01, 01, 2024, 02, 01)]
        [TestCase(2024, 01, 01, 2025, 01, 01)]
        public void DoIntsCompareCorrectly(int y1, int m1, int d1, int y2, int m2, int d2)
        {
            var testCase1 = new DateOnly(y1, m1, d1);
            var testCase2 = new DateOnly(y2, m2, d2);

            var int1 = testCase1.ToInt();
            var int2 = testCase2.ToInt();

            var comparisonResult = int1.CompareTo(int2);

            Assert.Less(comparisonResult, 0);
        }
    }
}