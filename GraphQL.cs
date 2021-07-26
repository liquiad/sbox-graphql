/*
	GraphQL Client for s&box
*/

using System;
using System.Collections.Generic;
using System.Net.Http;
using Sandbox;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json.Serialization;

namespace GraphQL
{
	public class GraphQLException : Exception { }

	public class GraphQLClient<QueryClass, MutationClass>
	{
		public string Endpoint { get; set; }
		public string QueriesPath { get; set; }
		public string MutationsPath { get; set; }
		private readonly Dictionary<string, string> Queries = new();
		private readonly Dictionary<string, string> Mutations = new();
		private static readonly HttpClient client = new();

		public GraphQLClient()
		{
		}

		public void LoadOperations()
		{
			IEnumerable<string> queryFiles = FileSystem.Mounted.FindFile( QueriesPath, "*.graphql" );

			foreach ( string file in queryFiles )
			{
				Queries.Add( file.Split( ".graphql" )[0], FileSystem.Mounted.ReadAllText( $"{QueriesPath}{file}" ) );
			}

			IEnumerable<string> mutationFiles = FileSystem.Mounted.FindFile( MutationsPath, "*.graphql" );

			foreach ( var file in mutationFiles )
			{
				Mutations.Add( file.Split( ".graphql" )[0], FileSystem.Mounted.ReadAllText( $"{MutationsPath}{file}" ) );
			}
		}

		public Task<GraphQLResponse<QueryClass>> Query( string name, object variables = null )
		{
			return Query<object>( name, variables );
		}

		public Task<GraphQLResponse<QueryClass>> Query<VariablesClass>( string name, VariablesClass variables = default )
		{
			if ( !Queries.ContainsKey( name ) )
			{
				throw new Exception( $"Could not find query '{name}'! Ensure that '{name}.graphql' exists in your queries folder!" );
			}

			string query = Queries[name];

			return DoRequest<QueryClass>( name, query, variables );
		}

		public Task<GraphQLResponse<MutationClass>> Mutate( string name, object variables = null )
		{
			return Mutate<object>( name, variables );
		}

		public Task<GraphQLResponse<MutationClass>> Mutate<VariablesClass>( string name, VariablesClass variables = default )
		{
			if ( !Mutations.ContainsKey( name ) )
			{
				throw new Exception( $"Could not find mutation '{name}'! Ensure that '{name}.graphql' exists in your mutations folder!" );
			}

			string mutation = Mutations[name];

			return DoRequest<MutationClass>( name, mutation, variables );
		}

		private class GraphQLRequest
		{
			[JsonPropertyName( "operationName" )]
			public string OperationName { get; set; }


			[JsonPropertyName( "query" )]
			public string Query { get; set; }

			[JsonPropertyName( "variables" )]
			public object Variables { get; set; }
		}

		public class GraphQLError
		{
			[JsonPropertyName( "message" )]
			public string Message { get; set; }
		}

		public class GraphQLResponse<T>
		{
			[JsonPropertyName( "data" )]
			public T Data { get; set; }

			[JsonPropertyName( "errors" )]
			public List<GraphQLError> Errors { get; set; }

			[JsonIgnore]
			public bool Success = false;
		}

		// TODO: Swap this out with s&box HTTP once they add support for POST requests 
		private async Task<GraphQLResponse<T>> DoRequest<T>( string operationName, string query, object variables )
		{
			GraphQLRequest request = new()
			{
				OperationName = operationName,
				Query = query,
				Variables = variables ?? new()
			};

			StringContent content = new( JsonSerializer.Serialize( request ), Encoding.UTF8, "application/json" );
			HttpResponseMessage response = await client.PostAsync( Endpoint, content );

			string responseString = await response.Content.ReadAsStringAsync();

			GraphQLResponse<T> gqlResponse = JsonSerializer.Deserialize<GraphQLResponse<T>>( responseString );

			if ( gqlResponse.Errors == null )
			{
				gqlResponse.Success = true;
			}
			else
			{
				Log.Error( $"[GraphQL] Error while calling '{operationName}'!" );
				foreach ( GraphQLError error in gqlResponse.Errors )
				{
					foreach ( string line in error.Message.Split( "\n" ) )
					{
						Log.Error( $"\t{line}" );
					}
				}
			}

			return gqlResponse;
		}
	}
}