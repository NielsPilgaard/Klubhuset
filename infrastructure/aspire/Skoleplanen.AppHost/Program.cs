const string label = "skoleplanen";

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL — shared server, separate databases for API and Keycloak
var pgUsername = builder.AddParameter("postgres-username", "postgres");
var pgPassword = builder.AddParameter("postgres-password", secret: true);

var postgres = builder.AddPostgres("postgres", userName: pgUsername, password: pgPassword)
					  .WithLifetime(ContainerLifetime.Persistent)
					  .WithPgAdmin(pgAdmin => pgAdmin.WithContainerRuntimeArgs(
									   "--label",
									   $"com.docker.compose.project={label}"))
					  .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={label}");

var db = postgres.AddDatabase("skoleplanen-db");
postgres.AddDatabase("keycloak-db");

// Keycloak (raw container — no first-party Aspire hosting package)
var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.2")
					  .WithLifetime(ContainerLifetime.Persistent)
					  .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
					  .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
					  .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD",
									   builder.AddParameter("keycloak-admin-password", secret: true))
					  .WithEnvironment("KC_DB", "postgres")
					  .WithEnvironment("KC_DB_USERNAME", pgUsername)
					  .WithEnvironment("KC_DB_PASSWORD", pgPassword)
					  .WithEnvironment("KC_DB_URL",
									   ReferenceExpression.Create(
										   $"jdbc:postgresql://{postgres.Resource.Host}:{postgres.Resource.Port}/keycloak-db"))
					  .WithBindMount("../../../infrastructure/keycloak/realms",
									 "/opt/keycloak/data/import",
									 isReadOnly: true)
					  .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={label}")
					  .WithArgs("start-dev", "--import-realm")
					  .WaitFor(postgres);

// LocalStack (S3-compatible local emulation for OVHCloud Object Storage)
builder.AddContainer("localstack", "localstack/localstack", "3")
	   .WithLifetime(ContainerLifetime.Persistent)
	   .WithHttpEndpoint(port: 4566, targetPort: 4566, name: "gateway")
	   .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={label}");

// API
var api = builder.AddProject<Projects.Skoleplanen_Api>("api")
				 .WithReference(db)
				 .WithReference(keycloak.GetEndpoint("http"))
				 .WaitFor(db)
				 .WaitFor(keycloak)
				 .WithUrlForEndpoint("http", url =>
			 {
				 url.Url += "/api/v1/openapi";
				 url.DisplayText = "Swagger UI";
			 });

// React + Vite frontend
builder.AddViteApp("web", appDirectory: "../../../web", runScriptName: "dev")
	   .WithEndpoint("http", e => e.Port = 5173)
	   .WithExternalHttpEndpoints()
	   .WithReference(api);

builder.Build().Run();
