using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ExceptionsPerformance {
    public class XmlParsingExample {
        private readonly ITestOutputHelper _output;
        public XmlParsingExample(ITestOutputHelper output) {
            _output = output;
        }

        [Fact]
        public void Build_sample_data() {
            double errorRate = 1; // 10% of the time our users mess up
            int count = 10; // 10000 entries by a user

            var doc = BuildSampleData(errorRate, count);
            _output.WriteLine(doc.ToString());
        }

        static XDocument BuildSampleData(double errorRate, int count) {
            Random random = new Random(1);
            string bad_prefix = @"X";

            XDocument doc = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XComment("Sample Data From Who Knows Where"),
            new XElement("sampleData"));

            for (int i = 0; i < count; i++) {
                string input = random.Next().ToString();
                if (random.NextDouble() < errorRate) {
                    input = bad_prefix + input;
                }
                var el = new XElement("item",
                    new XElement("property", new XAttribute("name", "Cost"), input)
                );
                doc.Element("sampleData").Add(el);
            }
            return doc;
        }
    }
}
