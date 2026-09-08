using System.Globalization;
using System.Text.Json.Nodes;
using System.Xml.Linq;

namespace MudBlazor.Extensions.Helper.Internal;

/// <summary>
/// Converts GPX and KML documents into a GeoJSON FeatureCollection so a single renderer can display all three
/// formats. Elements are matched by their local name, which keeps the conversion working across the different
/// GPX (1.0/1.1) and KML (2.0-2.3, including the google extension namespace) schema versions.
/// </summary>
internal static class GeoJsonConverter
{
    /// <summary>
    /// Converts the content of a .gpx, .kml or .geojson file into a GeoJSON string. GeoJSON input is returned
    /// unchanged, so the caller can hand any of the three straight to the map.
    /// </summary>
    public static string ToGeoJson(string fileName, string content)
    {
        if (fileName.EndsWith(".gpx", StringComparison.OrdinalIgnoreCase))
            return FromGpx(content).ToJsonString();
        if (fileName.EndsWith(".kml", StringComparison.OrdinalIgnoreCase))
            return FromKml(content).ToJsonString();
        return content;
    }

    private static JsonObject FromGpx(string content)
    {
        var document = XDocument.Parse(content);
        var features = new JsonArray();

        foreach (var waypoint in Elements(document, "wpt"))
        {
            var position = ReadLatLon(waypoint);
            if (position != null)
                features.Add(Feature(Geometry("Point", position), Name(waypoint), Description(waypoint)));
        }

        // A track can consist of several segments, each one is a line of its own.
        foreach (var track in Elements(document, "trk"))
        {
            var name = Name(track);
            foreach (var segment in track.Elements().Where(e => e.Name.LocalName == "trkseg"))
            {
                var line = Positions(segment, "trkpt");
                if (line.Count > 1)
                    features.Add(Feature(Geometry("LineString", line), name, Description(track)));
            }
        }

        foreach (var route in Elements(document, "rte"))
        {
            var line = Positions(route, "rtept");
            if (line.Count > 1)
                features.Add(Feature(Geometry("LineString", line), Name(route), Description(route)));
        }

        return FeatureCollection(features);
    }

    private static JsonObject FromKml(string content)
    {
        var document = XDocument.Parse(content);
        var features = new JsonArray();

        foreach (var placemark in Elements(document, "Placemark"))
        {
            var name = Name(placemark);
            var description = Description(placemark);

            foreach (var geometry in placemark.Descendants().Where(e => e.Name.LocalName is "Point" or "LineString" or "Polygon"))
            {
                var geoJson = FromKmlGeometry(geometry);
                if (geoJson != null)
                    features.Add(Feature(geoJson, name, description));
            }
        }

        return FeatureCollection(features);
    }

    private static JsonObject FromKmlGeometry(XElement geometry)
    {
        switch (geometry.Name.LocalName)
        {
            case "Point":
                var point = KmlCoordinates(geometry).FirstOrDefault();
                return point == null ? null : Geometry("Point", point);

            case "LineString":
                var line = new JsonArray();
                foreach (var position in KmlCoordinates(geometry))
                    line.Add(position);
                return line.Count > 1 ? Geometry("LineString", line) : null;

            case "Polygon":
                // GeoJSON polygons are an array of rings, the outer boundary first.
                var rings = new JsonArray();
                foreach (var boundary in geometry.Descendants().Where(e => e.Name.LocalName == "LinearRing"))
                {
                    var ring = new JsonArray();
                    foreach (var position in KmlCoordinates(boundary))
                        ring.Add(position);
                    if (ring.Count > 3)
                        rings.Add(ring);
                }
                return rings.Count > 0 ? Geometry("Polygon", rings) : null;

            default:
                return null;
        }
    }

    /// <summary>
    /// KML packs all positions of a geometry into one whitespace separated list of "lon,lat[,altitude]" tuples.
    /// </summary>
    private static IEnumerable<JsonArray> KmlCoordinates(XElement geometry)
    {
        var raw = geometry.Descendants().FirstOrDefault(e => e.Name.LocalName == "coordinates")?.Value;
        if (string.IsNullOrWhiteSpace(raw))
            yield break;

        foreach (var tuple in raw.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = tuple.Split(',');
            if (parts.Length >= 2
                && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude)
                && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude))
                yield return new JsonArray(longitude, latitude);
        }
    }

    private static IEnumerable<XElement> Elements(XDocument document, string localName)
        => document.Descendants().Where(e => e.Name.LocalName == localName);

    private static JsonArray Positions(XElement parent, string localName)
    {
        var positions = new JsonArray();
        foreach (var position in parent.Descendants().Where(e => e.Name.LocalName == localName).Select(ReadLatLon).Where(p => p != null))
            positions.Add(position);
        return positions;
    }

    /// <summary>GPX carries the position in attributes and, unlike GeoJSON, latitude first.</summary>
    private static JsonArray ReadLatLon(XElement element)
    {
        var lat = element.Attribute("lat")?.Value;
        var lon = element.Attribute("lon")?.Value;
        return double.TryParse(lat, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitude)
               && double.TryParse(lon, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitude)
            ? new JsonArray(longitude, latitude)
            : null;
    }

    private static string Name(XElement element)
        => element.Elements().FirstOrDefault(e => e.Name.LocalName == "name")?.Value?.Trim();

    private static string Description(XElement element)
        => element.Elements().FirstOrDefault(e => e.Name.LocalName is "desc" or "description")?.Value?.Trim();

    private static JsonObject Geometry(string type, JsonNode coordinates)
        => new() { ["type"] = type, ["coordinates"] = coordinates };

    private static JsonObject Feature(JsonNode geometry, string name, string description)
    {
        var properties = new JsonObject();
        if (!string.IsNullOrEmpty(name))
            properties["name"] = name;
        if (!string.IsNullOrEmpty(description))
            properties["description"] = description;

        return new JsonObject
        {
            ["type"] = "Feature",
            ["properties"] = properties,
            ["geometry"] = geometry
        };
    }

    private static JsonObject FeatureCollection(JsonArray features)
        => new() { ["type"] = "FeatureCollection", ["features"] = features };
}
