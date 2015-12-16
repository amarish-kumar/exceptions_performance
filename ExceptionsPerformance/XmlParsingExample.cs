using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
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
            double errorRate = .5; // 50% of the time our users mess up
            int count = 50000; // 50000 entries by a user

            var doc = BuildSampleDataBadString(errorRate, count);

            _output.WriteLine(doc.ToString());
            //<!--Sample Data from Somewhere-->
            //<SampleData>
            //  <Item>
            //    <property name="ItemId" value="0" />
            //    <property name="ItemDescription" value="ItemId: 0 Desc" />
            //    <property name="ItemCode" value="P123-456-0" />
            //    <property name="ItemCost" value="X534011718" /> // BAD...
            //  </Item>
            //  <Item>
            //    <property name="ItemId" value="1" />
            //    <property name="ItemDescription" value="ItemId: 1 Desc" />
            //    <property name="ItemCode" value="P123-456-1" />
            //    <property name="ItemCost" value="1002897798" />
            //  </Item>
            //  <Item>
            //    <property name="ItemId" value="2" />
            //    <property name="ItemDescription" value="ItemId: 2 Desc" />
            //    <property name="ItemCode" value="P123-456-2" />
            //    <property name="ItemCost" value="X1412011072" /> // BAD...
            //  </Item>
            //  ...
        }

        [Fact]
        public void Deserialize_xml_to_item_class_base_throw_error() {
            var externalData = BuildSampleDataBadString(1, 10);

            var deserializedRawData = DeserializeRawData(externalData);
            var domainModelItems = deserializedRawData.Select(x => new List<ItemEntity>() {
                new ItemEntity() {
                    ItemId = x.GetPropertyValue("ItemCode"),
                    ItemDescription = x.GetPropertyValue("ItemDescription"),
                    ItemCode = x.GetPropertyValue("ItemCode"),
                    ItemCost = int.Parse(x.GetPropertyValue("ItemCost"))
                }
            });

            Assert.Throws<System.FormatException>(() => domainModelItems.ToList());
        }

        [Fact]
        public void Deserialize_xml_to_item_class_no_linq_try_catch() {
            var tryCatchTime = TimeTryCatch(1, 5000);

            int test;
            var testP = int.TryParse(null, out test);

            _output.WriteLine(tryCatchTime.ToString());
        }

        [Fact]
        public void Deserialize_xml_to_item_class_no_linq_try_parse() {
            var tryCatchTime = TimeTryParse(1, 5000);
            _output.WriteLine(tryCatchTime.ToString());
        }

        [Fact]
        public void Benchmarks() {
            _output.WriteLine("FailureRate  Try-Catch           TryParse         Difference");
            for (double i = 0; i < 1; i += .1) {
                double errorRate = i; // % of the time our users mess up
                int count = 50000; // # entries by a user

                TimeSpan trycatch = TimeTryCatch(errorRate, count);
                TimeSpan tryparse = TimeTryParse(errorRate, count);

                //_output.WriteLine("trycatch: {0}", trycatch);
                //_output.WriteLine("tryparse: {0}", tryparse);
                //_output.WriteLine("slowdown: {0}", trycatch.Subtract(tryparse));
                //_output.WriteLine(Environment.NewLine);


                _output.WriteLine(String.Format("{0:P}      {1}    {2} {3}", i, trycatch, tryparse, trycatch.Subtract(tryparse)));
            }
        }

        [Fact]
        public void Benchmark_try_catch() {
            _output.WriteLine("FailureRate  ExecutionTime");
            for (double i = 0; i < 1; i += .1) {
                double errorRate = i; // % of the time our users mess up
                int count = 50000; // # entries by a user

                TimeSpan trycatch = TimeTryCatch(errorRate, count);
                _output.WriteLine("{0:P}    {1}", errorRate, trycatch);
            }
        }


        [Fact]
        public void Benchmark_tryparse() {
            _output.WriteLine("FailureRate  ExecutionTime");
            for (double i = 0; i < 1; i += .1) {
                double errorRate = i; // % of the time our users mess up
                int count = 50000; // # entries by a user

                TimeSpan trycatch = TimeTryParse(errorRate, count);
                _output.WriteLine("{0:P}    {1}", errorRate, trycatch);
            }
        }

        public TimeSpan TimeTryCatch(double errorRate, int count) {
            Stopwatch stopwatch = new Stopwatch();
            var externalData = BuildSampleDataBadString(errorRate, count);
            List<Item> deserializedRawData = DeserializeRawData(externalData);
            var itemEntities = new List<ItemEntity>();

            stopwatch.Start();
            foreach (var item in deserializedRawData) {
                var itemEntity = new ItemEntity();
                itemEntity.ItemId = item.GetPropertyValue("ItemId");
                itemEntity.ItemDescription = item.GetPropertyValue("ItemDescription");
                itemEntity.ItemCode = item.GetPropertyValue("ItemCode");
                try {
                    itemEntity.ItemCost = int.Parse(item.GetPropertyValue("ItemCost"));
                } catch (Exception) {
                    itemEntity.ItemCost = 0;
                }
                itemEntities.Add(itemEntity);
            }
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        public TimeSpan TimeTryParse(double errorRate, int count) {
            Stopwatch stopwatch = new Stopwatch();
            var externalData = BuildSampleDataBadString(errorRate, count);
            List<Item> deserializedRawData = DeserializeRawData(externalData);
            var itemEntities = new List<ItemEntity>();

            stopwatch.Start();
            foreach (var item in deserializedRawData) {
                var itemEntity = new ItemEntity();
                itemEntity.ItemId = item.GetPropertyValue("ItemId");
                itemEntity.ItemDescription = item.GetPropertyValue("ItemDescription");
                itemEntity.ItemCode = item.GetPropertyValue("ItemCode");

                int itemCost; 
                int.TryParse(item.GetPropertyValue("ItemCode"), out itemCost);
                itemEntity.ItemCost = itemCost;

                itemEntities.Add(itemEntity);
            }
            stopwatch.Stop();
            return stopwatch.Elapsed;
        }

        private static XDocument BuildSampleDataBadString(double errorRate, int count) {
            Random random = new Random(1);
            string bad_prefix = @"X";

            XDocument doc = new XDocument(
                new XDeclaration("1.0", "utf-8", "yes"),
                new XComment("Sample Data from Somewhere"),
                new XElement("SampleData"));

            for (int i = 0; i < count; i++) {
                string randomInput = random.Next().ToString();
                double errorSwitch = random.NextDouble();
                if (errorSwitch < errorRate) {
                    randomInput = bad_prefix + randomInput;
                }
                var el = new XElement("Item",
                    new XElement("property",
                        new XAttribute("name", "ItemId"),
                        new XAttribute("value", i.ToString())),
                    new XElement("property",
                        new XAttribute("name", "ItemDescription"),
                        new XAttribute("value", "ItemId: " + i + " Desc")),
                    new XElement("property",
                        new XAttribute("name", "ItemCode"),
                        new XAttribute("value", "P123-456-" + i)),
                    // Here's where the data gets corrupted
                    new XElement("property",
                        new XAttribute("name", "ItemCost"),
                        new XAttribute("value", randomInput))
                    );
                doc.Element("SampleData").Add(el);
            }
            return doc;
        }

        public List<Item> DeserializeRawData(XDocument rawData) {
            var itemsXml = rawData.Element("SampleData");
            var serializer = new XmlSerializer(typeof(Item));
            var itemsElements = itemsXml.Elements();

            var items = itemsElements
                .Select(x => (Item)serializer.Deserialize(x.CreateReader()))
                .ToList();
            return items;
        }
    }

    public static class XmlExtensionMethods {
        public static string GetPropertyValue(this Item item, string name) {
            return item.properties
                .Where(x => x.name == name)
                .Select(y => y.value)
                .FirstOrDefault();
        }
    }

    public class ItemEntity {
        public string ItemId { get; set; }
        public string ItemDescription { get; set; }
        public string ItemCode { get; set; }
        public int ItemCost { get; set; }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    [System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
    public partial class Item {

        private ItemProperty[] propertyField;

        /// <remarks/>
        [System.Xml.Serialization.XmlElementAttribute("property")]
        public ItemProperty[] properties {
            get {
                return this.propertyField;
            }
            set {
                this.propertyField = value;
            }
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
    public partial class ItemProperty {

        private string nameField;

        private string valueField;

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string name {
            get {
                return this.nameField;
            }
            set {
                this.nameField = value;
            }
        }

        /// <remarks/>
        [System.Xml.Serialization.XmlAttributeAttribute()]
        public string value {
            get {
                return this.valueField;
            }
            set {
                this.valueField = value;
            }
        }
    }
}
