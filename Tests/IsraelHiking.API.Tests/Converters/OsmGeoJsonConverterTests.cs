﻿using System.Linq;
using GeoJSON.Net.Geometry;
using IsraelHiking.API.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OsmSharp.Collections.Tags;
using OsmSharp.Osm;

namespace IsraelHiking.API.Tests.Converters
{
    [TestClass]
    public class OsmGeoJsonConverterTests
    {
        private const string NAME = "name";
        private OsmGeoJsonConverter _converter;

        private Node CreateNode(int number)
        {
            return new Node
            {
                Longitude = number,
                Latitude = number,
                Id = number
            };
        }

        [TestInitialize]
        public void TestInitialize()
        {
            _converter = new OsmGeoJsonConverter();
        }

        [TestMethod]
        public void ToGeoJson_NoTags_ShouldReturnNull()
        {
            var feature = _converter.ToGeoJson(new Node { Tags = new TagsCollection() });

            Assert.IsNull(feature);
        }

        [TestMethod]
        public void ToGeoJson_Node_ShouldReturnPoint()
        {
            var node = CreateNode(1);
            node.Tags = new TagsCollection(new Tag(NAME, NAME));

            var feature = _converter.ToGeoJson(node);
            var point = feature.Geometry as Point;

            Assert.AreEqual(feature.Properties[NAME], NAME);
            Assert.IsNotNull(point);
            var position = point.Coordinates as GeographicPosition;
            Assert.IsNotNull(position);
            Assert.AreEqual(node.Latitude, position.Latitude);
            Assert.AreEqual(node.Longitude, position.Longitude);
        }

        [TestMethod]
        public void ToGeoJson_WayWithOneNode_ShouldReturnNull()
        {
            var node = CreateNode(1);
            var way = CompleteWay.Create(2);
            way.Nodes.Add(node);
            way.Tags.Add(NAME, NAME);

            var feature = _converter.ToGeoJson(way);

            Assert.IsNull(feature);
        }


        [TestMethod]
        public void ToGeoJson_Way_ShouldReturnLineString()
        {
            var node1 = CreateNode(1);
            var node2 = CreateNode(2);
            var way = CompleteWay.Create(3);
            way.Nodes.Add(node1);
            way.Nodes.Add(node2);
            way.Tags.Add(NAME, NAME);

            var feature = _converter.ToGeoJson(way);
            var lineString = feature.Geometry as LineString;

            Assert.AreEqual(feature.Properties[NAME], NAME);
            Assert.IsNotNull(lineString);
            Assert.AreEqual(node1.Latitude, lineString.Coordinates.OfType<GeographicPosition>().First().Latitude);
            Assert.AreEqual(node2.Longitude, lineString.Coordinates.OfType<GeographicPosition>().Last().Longitude);
        }

        [TestMethod]
        public void ToGeoJson_Way_ShouldReturnPolygon()
        {
            var node1 = CreateNode(1);
            var node2 = CreateNode(2);
            var node3 = CreateNode(3);
            var node4 = CreateNode(1);
            var way = CompleteWay.Create(4);
            way.Nodes.AddRange(new[] { node1, node2, node3, node4 });
            way.Tags.Add(NAME, NAME);

            var feature = _converter.ToGeoJson(way);
            var polygon = feature.Geometry as Polygon;

            Assert.AreEqual(feature.Properties[NAME], NAME);
            Assert.IsNotNull(polygon);
            Assert.AreEqual(node1.Latitude, polygon.Coordinates.SelectMany(c => c.Coordinates).OfType<GeographicPosition>().First().Latitude);
            Assert.AreEqual(node1.Longitude, polygon.Coordinates.SelectMany(c => c.Coordinates).OfType<GeographicPosition>().Last().Longitude);
        }

        [TestMethod]
        public void ToGeoJson_RelationWithOuterOnly_ShouldReturnMultiPolygon()
        {
            var node1 = CreateNode(1);
            var node2 = CreateNode(2);
            var node3 = CreateNode(3);
            var node4 = CreateNode(1);
            var way = CompleteWay.Create(4);
            way.Nodes.AddRange(new[] { node1, node2, node3, node4 });
            var relation = CompleteRelation.Create(5);
            relation.Tags.Add("boundary", "true");
            relation.Members.Add(new CompleteRelationMember { Member = way, Role = "outer" });

            var feature = _converter.ToGeoJson(relation);
            var multiPolygon = feature.Geometry as MultiPolygon;

            Assert.IsNotNull(multiPolygon);
            Assert.AreEqual(1, multiPolygon.Coordinates.Count);
            Assert.AreEqual(node1.Latitude, multiPolygon.Coordinates.First().Coordinates.SelectMany(p => p.Coordinates).OfType<GeographicPosition>().First().Latitude);
            Assert.AreEqual(node4.Longitude, multiPolygon.Coordinates.First().Coordinates.SelectMany(p => p.Coordinates).OfType<GeographicPosition>().Last().Longitude);
        }

        [TestMethod]
        public void ToGeoJson_RelationWithOuterAndInner_ShouldReturnMultiPolygon()
        {
            var node1 = CreateNode(1);
            var node2 = CreateNode(2);
            var node3 = CreateNode(3);
            var node4 = CreateNode(1);
            var way = CompleteWay.Create(4);
            way.Nodes.AddRange(new[] { node1, node2, node3, node4 });
            var relation = CompleteRelation.Create(5);
            relation.Tags.Add("type", "boundary");
            relation.Members.Add(new CompleteRelationMember { Member = way, Role = "outer" });
            relation.Members.Add(new CompleteRelationMember { Member = way, Role = "inner" });

            var feature = _converter.ToGeoJson(relation);
            var multiPolygon = feature.Geometry as MultiPolygon;

            Assert.IsNotNull(multiPolygon);
            Assert.AreEqual(2, multiPolygon.Coordinates.Count);
            Assert.AreEqual(node1.Latitude, multiPolygon.Coordinates.First().Coordinates.SelectMany(p => p.Coordinates).OfType<GeographicPosition>().First().Latitude);
            Assert.AreEqual(node4.Longitude, multiPolygon.Coordinates.Last().Coordinates.SelectMany(p => p.Coordinates).OfType<GeographicPosition>().Last().Longitude);
        }

        [TestMethod]
        public void ToGeoJson_RelationWithTwoSubRelationsWithInnerRole_ShouldReturnMultiPolygon()
        {
            var node1 = CreateNode(1);
            var node2 = CreateNode(2);
            var node3 = CreateNode(3);
            var node4 = CreateNode(1);
            var way = CompleteWay.Create(4);
            way.Nodes.AddRange(new[] { node1, node2, node3, node4 });
            var subRelation1 = CompleteRelation.Create(5);
            subRelation1.Members.Add(new CompleteRelationMember { Member = way, Role = "outer" });
            var subRelation2 = CompleteRelation.Create(5);
            subRelation2.Members.Add(new CompleteRelationMember { Member = way, Role = "outer" });
            var relation = CompleteRelation.Create(5);
            relation.Tags.Add("type", "multipolygon");
            relation.Members.Add(new CompleteRelationMember { Member = subRelation1 });
            relation.Members.Add(new CompleteRelationMember { Member = subRelation2 });

            var feature = _converter.ToGeoJson(relation);
            var multiPolygon = feature.Geometry as MultiPolygon;

            Assert.IsNotNull(multiPolygon);
            Assert.AreEqual(1, multiPolygon.Coordinates.Count);
            Assert.AreEqual(node1.Latitude, multiPolygon.Coordinates.First().Coordinates.SelectMany(p => p.Coordinates).OfType<GeographicPosition>().First().Latitude);
            Assert.AreEqual(node4.Longitude, multiPolygon.Coordinates.Last().Coordinates.SelectMany(p => p.Coordinates).OfType<GeographicPosition>().Last().Longitude);
        }

        [TestMethod]
        public void ToGeoJson_RelationWithNodes_ShouldReturnMultiPoint()
        {
            var node1 = CreateNode(1);
            var node2 = CreateNode(2);
            var relation = CompleteRelation.Create(3);
            relation.Tags.Add(NAME, NAME);
            relation.Members.Add(new CompleteRelationMember { Member = node1 });
            relation.Members.Add(new CompleteRelationMember { Member = node2 });

            var feature = _converter.ToGeoJson(relation);
            var multiPoint = feature.Geometry as MultiPoint;

            Assert.IsNotNull(multiPoint);
            Assert.AreEqual(2, multiPoint.Coordinates.Count);
            var position1 = multiPoint.Coordinates.First().Coordinates as GeographicPosition;
            Assert.IsNotNull(position1);
            Assert.AreEqual(node1.Latitude, position1.Latitude);
            var position2 = multiPoint.Coordinates.Last().Coordinates as GeographicPosition;
            Assert.IsNotNull(position2);
            Assert.AreEqual(node2.Longitude, position2.Longitude);
        }

        [TestMethod]
        public void ToGeoJson_RelationWithoutWays_ShouldReturnMultiPoint()
        {
            var relation = CompleteRelation.Create(3);
            relation.Tags.Add(NAME, NAME);

            var feature = _converter.ToGeoJson(relation);

            Assert.IsNull(feature);
        }

        [TestMethod]
        public void ToGeoJson_RelationWithPolygonAndLineString_ShouldReturnMultiLineStringAfterGrouping()
        {
            var node1 = CreateNode(1);
            var node2 = CreateNode(2);
            var node3 = CreateNode(3);
            var node4 = CreateNode(4);
            var node5 = CreateNode(5);
            var node6 = CreateNode(6);
            var node7 = CreateNode(7);
            var wayPartOfLineString1 = CompleteWay.Create(8);
            var wayPartOfLineString2 = CompleteWay.Create(9);
            wayPartOfLineString1.Nodes.AddRange(new[] { node1, node2 });
            wayPartOfLineString2.Nodes.AddRange(new[] { node2, node3 });
            var wayPartOfPolygon1 = CompleteWay.Create(10);
            var wayPartOfPolygon2 = CompleteWay.Create(11);
            wayPartOfPolygon1.Nodes.AddRange(new[] { node4, node5, node6 });
            wayPartOfPolygon2.Nodes.AddRange(new[] { node4, node7, node6 });
            var relation = CompleteRelation.Create(12);
            relation.Tags.Add(NAME, NAME);
            relation.Members.Add(new CompleteRelationMember { Member = wayPartOfLineString1 });
            relation.Members.Add(new CompleteRelationMember { Member = wayPartOfLineString2 });
            relation.Members.Add(new CompleteRelationMember { Member = wayPartOfPolygon1 });
            relation.Members.Add(new CompleteRelationMember { Member = wayPartOfPolygon2 });

            var feature = _converter.ToGeoJson(relation);
            var multiLineString = feature.Geometry as MultiLineString;

            Assert.IsNotNull(multiLineString);
            Assert.AreEqual(2, multiLineString.Coordinates.Count);
            var lineString = multiLineString.Coordinates.First();
            Assert.AreEqual(3, lineString.Coordinates.Count);
            var polygon = multiLineString.Coordinates.Last();
            Assert.AreEqual(5, polygon.Coordinates.Count);
            Assert.AreEqual(polygon.Coordinates.First(), polygon.Coordinates.Last());
        }

        [TestMethod]
        public void ToGeoJson_RelationWithRelation_ShouldReturnMultiLineStringAfterGrouping()
        {
            var node1 = CreateNode(1);
            var node2 = CreateNode(2);
            var node3 = CreateNode(3);
            var node4 = CreateNode(4);
            var node5 = CreateNode(5);
            var node6 = CreateNode(6);
            var node7 = CreateNode(7);

            var wayPartOfLineString1 = CompleteWay.Create(8);
            var wayPartOfLineString2 = CompleteWay.Create(9);

            wayPartOfLineString1.Nodes.AddRange(new[] { node2, node3 });
            wayPartOfLineString2.Nodes.AddRange(new[] { node1, node2 });

            var wayPartOfPolygon1 = CompleteWay.Create(10);
            var wayPartOfPolygon2 = CompleteWay.Create(11);

            wayPartOfPolygon1.Nodes.AddRange(new[] { node4, node5, node6 });
            wayPartOfPolygon2.Nodes.AddRange(new[] { node4, node7, node6 });

            var subRelation = CompleteRelation.Create(12);
            subRelation.Members.Add(new CompleteRelationMember { Member = wayPartOfLineString1 });
            subRelation.Members.Add(new CompleteRelationMember { Member = wayPartOfLineString2 });
            var relation = CompleteRelation.Create(13);
            relation.Tags.Add(NAME, NAME);
            relation.Members.Add(new CompleteRelationMember { Member = subRelation });
            relation.Members.Add(new CompleteRelationMember { Member = wayPartOfPolygon1 });
            relation.Members.Add(new CompleteRelationMember { Member = wayPartOfPolygon2 });


            var feature = _converter.ToGeoJson(relation);
            var multiLineString = feature.Geometry as MultiLineString;

            Assert.IsNotNull(multiLineString);
            Assert.AreEqual(2, multiLineString.Coordinates.Count);
            var lineString = multiLineString.Coordinates.First();
            Assert.AreEqual(3, lineString.Coordinates.Count);
            var polygon = multiLineString.Coordinates.Last();
            Assert.AreEqual(5, polygon.Coordinates.Count);
            Assert.AreEqual(polygon.Coordinates.First(), polygon.Coordinates.Last());
        }

    }
}