using MongoDB.Bson;
using MongoDB.Driver;

// For a replica set, include the replica set name and a seedlist of the members in the URI string; e.g.
string uri = "mongodb://localhost:27017/?replicaSet=rs0";
// For a sharded cluster, connect to the mongos instances; e.g.
// string uri = "mongodb://mongos0.example.com:27017,mongos1.example.com:27017/";
var client = new MongoClient(uri);
// Prereq: Create collections.
var database1 = client.GetDatabase("mydb1");
var collection1 = database1.GetCollection<BsonDocument>("foo").WithWriteConcern(WriteConcern.WMajority);
collection1.InsertOne(new BsonDocument("abc", 0));
var database2 = client.GetDatabase("mydb2");
var collection2 = database2.GetCollection<BsonDocument>("bar").WithWriteConcern(WriteConcern.WMajority);
collection2.InsertOne(new BsonDocument("xyz", 0));
// Step 1: Start a client session.
using (var session = client.StartSession())
{
	// Step 2: Optional. Define options to use for the transaction.
	var transactionOptions = new TransactionOptions(
			readPreference: ReadPreference.Primary,
			readConcern: ReadConcern.Local,
			writeConcern: WriteConcern.WMajority);
	// Step 3: Define the sequence of operations to perform inside the transactions
	var cancellationToken = CancellationToken.None; // normally a real token would be used
	var result = session.WithTransaction(
			(s, ct) =>
			{
				collection1.InsertOne(s, new BsonDocument("abc", 1), cancellationToken: ct);
				collection2.InsertOne(s, new BsonDocument("xyz", 999), cancellationToken: ct);

				throw new Exception("Some error");

				return "Inserted into collections in different databases";
			},
			transactionOptions,
			cancellationToken);
}
// check dbs, there will be 2 records totally