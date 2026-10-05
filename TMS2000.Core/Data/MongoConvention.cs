using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace TMS2000.Core.Data;

public static class MongoConventions
{
    private static bool _registered; // pour pas le faire 2 fois (quand il fait register la 1ere fois, il passe le truc en true//

    public static void Register()
    {
        if (_registered) return;

        var pack = new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new EnumRepresentationConvention(BsonType.String),
            new IgnoreExtraElementsConvention(true)
        };
        
        ConventionRegistry.Register("TMS2000", pack, _ => true); // meaning => applique ça à tout les types.

        BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128)); // fix le problème des décimals mongoDB.

        _registered = true;
    }
}