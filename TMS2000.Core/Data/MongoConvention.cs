using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.Serializers;

namespace TMS2000.Core.Data;

public static class MongoConventions
{
    private static readonly object RegistrationLock = new();
    private static bool _registered; // pour pas le faire 2 fois

    public static void Register()
    {
        // Un seul thread à la fois dans ce bloc : la vérification et l'enregistrement
        // deviennent une seule étape, impossible à couper en deux par un autre thread.

        lock (RegistrationLock)
        {
            if (_registered) return;

            var pack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new EnumRepresentationConvention(BsonType.String),
                new IgnoreExtraElementsConvention(true)
            };

            ConventionRegistry.Register("TMS2000", pack, _ => true); // applique ça à tout ce qui est dans la BDD.

            BsonSerializer.RegisterSerializer(new DecimalSerializer(BsonType.Decimal128)); // fix le problème des décimals mongoDB.

            _registered = true;
        }
    }
}