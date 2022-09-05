using GtfsProvider.Common.Extensions;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GtfsProvider.Tests
{
    public class StringExtensionTests
    {
        [Test]
        [TestCase("Dąbie", "Dąbie", true)]
        [TestCase("Dąbie", "dą", true)]
        [TestCase("Dąbie", "bie", true)]
        [TestCase("Teatr Bagatela", "bagate", true)]
        [TestCase("Dąbie", "Dąbiec", false)]
        [TestCase("Teatr Bagatela", "Teatr Słow", false)]
        public void SimpleMatch(string name, string query, bool result)
        {
            Assert.AreEqual(result, name.Matches(query));
        }

        [Test]
        [TestCase("Teatr Bagatela", "t b", true)]
        [TestCase("Teatr Bagatela", "te ba", true)]
        [TestCase("Teatr Bagatela", "t baga", true)]
        [TestCase("Dworzec Główny Zachód", "d g z", true)]
        [TestCase("Dworzec Główny Zachód", "d z", true)]
        [TestCase("Dworzec Główny Zachód", "d z x", false)]
        [TestCase("Dworzec Główny Zachód", "d g w", false)]
        [TestCase("Teatr Bagatela", "t Bagr", false)]
        public void MultipleWordMatch(string name, string query, bool result)
        {
            Assert.AreEqual(result, name.Matches(query));
        }

        [Test]
        [TestCase("Dworzec Główny Zachód", "d gl", true)]
        [TestCase("Dworzec Główny Zachód", "Glowny", true)]
        [TestCase("Zażółć gęślą jaźń", "zazolc gesla jazn", true)]
        [TestCase("Gojazni đačić s biciklom drži hmelj i finu vatu u džepu nošnje", "Gojazni djacic s biciklom drzi hmelj i finu vatu u dzepu nosnje", true)]
        [TestCase("Příliš žluťoučký kůň úpěl ďábelské ódy", "Prilis zlutoucky kun upel djabelske ody", true)]
        [TestCase("quäkt Jürgen blöd vom Paß", "quakt Jurgen blod vom Pas", true)]
        [TestCase("Árvíztűrő tükörfúrógép", "arvizturo tukorfurogep", true)]
        [TestCase("Muzicologă în bej vând whisky și tequila, preț fix", "Muzicologa in bej vand whisky si tequila, pret fix", true)]
        public void Accents(string name, string query, bool result)
        {
            Assert.AreEqual(result, name.Matches(query));
        }
    }
}