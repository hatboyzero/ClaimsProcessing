﻿using CoreClaims.Infrastructure.Domain.Entities;
using Microsoft.Azure.Cosmos;

namespace CoreClaims.Infrastructure.Repository
{
    public class AdjudicatorRepository : CosmosDbRepository, IAdjudicatorRepository
    {
        public AdjudicatorRepository(CosmosClient client) :
            base(client, "Adjudicator")
        {
        }

        public async Task<(IEnumerable<ClaimHeader>, int)> GetAssignedClaims(string adjudicatorId, int offset = 0, int limit = Constants.DefaultPageSize)
        {
            const string countSql = @"
                            SELECT VALUE COUNT(1) FROM c
                            WHERE c.adjudicatorId = @adjudicatorId AND c.type = 'ClaimHeader'";

            var countQuery = new QueryDefinition(countSql)
                .WithParameter("@adjudicatorId", adjudicatorId);

            var countResult = await Container.GetItemQueryIterator<int>(countQuery).ReadNextAsync();
            var count = countResult.Resource.FirstOrDefault();

            // Update the original query to include the count query parameters and return the results as a tuple
            const string sql = @"
                            SELECT * FROM c
                            WHERE c.adjudicatorId = @adjudicatorId AND c.type = 'ClaimHeader'
                            OFFSET @offset LIMIT @limit";

            var query = new QueryDefinition(sql)
                .WithParameter("@offset", offset)
                .WithParameter("@limit", limit)
                .WithParameter("@adjudicatorId", adjudicatorId);

            var result = await Query<ClaimHeader>(query);

            return (result, count);
        }


        public async Task<Adjudicator> GetRandomAdjudicator(string role = "Adjudicator", int offset = 0, int limit = Constants.DefaultPageSize)
        {
            var query = new QueryDefinition("SELECT * FROM a WHERE a.role = @role AND a.type = 'Adjudicator' OFFSET @offset LIMIT @limit")
                .WithParameter("@role", role)
                .WithParameter("@offset", offset)
                .WithParameter("@limit", limit);

            var result = (await Query<Adjudicator>(query)).ToList();

            if (result.Any())
            {
                return result.ElementAt(new Random().Next(1, result.Count) - 1);
            }

            return null;
        }

        public Task<Adjudicator> GetAdjudicator(string adjudicatorId)
        {
            return ReadItem<Adjudicator>(adjudicatorId, adjudicatorId);
        }

        public async Task UpsertClaim(ClaimHeader claim)
        {
            await Container.UpsertItemAsync(claim);
        }
    }
}
