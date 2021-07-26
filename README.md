# sbox-graphql

A basic GraphQL client for s&box.
It lets you do queries and mutations, and generate C# types from your GraphQL schema/Apollo server.
The codegen is using a modified version of the `c-sharp` plugin for [graphql-code-generator](https://github.com/dotansimha/graphql-code-generator/), with some stuff stripped out.

**Caveats:**

- Only dedicated servers right now. Your `accessgroups` must be modified. This will be changed once s&box has proper HTTP support.
- Only supports queries and mutations, no subscriptions as of yet.
- Must put queries and mutations in individual files (`mutations/*.graphql` and `queries/*.graphql`).
  _This was a deliberate design choice, so that you aren't dealing with writing shitty queries in verbatim string literals._
- Operation names must match their file names. See below.
- Your GraphQL server must have at-least _one_ query and _one_ mutation.

**Installing**

The type generation requires Node.js. I recommend [LTS](https://nodejs.org/en/).

1. Clone this repo into your **code** folder, into a folder called **gql**
   `cd addons/<your addon>/code/`
   `git clone https://github.com/liquiad/sbox-graphql gql`

2. Run `npm install` or `yarn`

3. Configure your GraphQL codegen in **codegen.yml**
   `npm run codegen` or `yarn codegen`

**Usage**

1. I recommend you create a copy of the class within your own namespace, that specifies your generated GraphQL types:

   ```cs
   public class GraphQLClient : GraphQL.GraphQLClient<GraphQLExample.APITypes.Query, GraphQLExample.APITypes.Mutation> { }
   ```

2. Construct the GraphQLClient like so

   ```cs
   [Library( "minimal" )]
   public partial class MinimalGame : Game
   {
   	private static GraphQLClient GQL;

   	public MinimalGame()
   	{
   		if ( IsServer )
   		{
   			SetupGraphQL();
   		}
   	}

   	[Event.Hotload] // Set it up again on hotload for development purposes
   	public static void SetupGraphQL()
   	{
   		GQL = new()
   		{
   			Endpoint = "http://localhost:4000/graphql",

   			// These paths must be relative to 'code/'
   			QueriesPath = "/gql/queries/",
   			MutationsPath = "/gql/mutations/"
   		};
   		GQL.LoadOperations();
   	}
   ```

3. Create a query or mutation
    **Example 1 (simple query):**
    ```graphql
    # This goes inside of /code/gql/queries/Hello.graphql
    query Hello {
        hello {
            world
        }
    }
    ```

    ```cs
    [ServerCmd( "graphql_hello_world" )]
	public static async void GraphQLHelloWorld()
	{
		// Now we reference the query by its file name
		var res = await GQL.Query( "Hello" );

		if ( res.Success )
		{
			Log.Info( res.Data.hello.world );
		}
	}
    ```

    **Example 2 (with variables):**
    ```graphql
    # This goes inside of /code/gql/queries/Echo.graphql
    query Echo($str: String!) {
        echo(str: $str)
    }
    ```

    ```cs
    [ServerCmd( "graphql_echo" )]
	public static async void GraphQLEcho()
	{
		var res = await GQL.Query( "Echo", new
		{
		    str = "Hello world!"
		} );

		if ( res.Success )
		{
			Log.Info( res.Data.echo );
		}
	}
    ```