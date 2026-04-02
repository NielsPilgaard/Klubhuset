const string label = "skoleplanen";

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL — shared server, separate databases for API and Keycloak
var postgres = builder.AddPostgres("postgres")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(pgAdmin => pgAdmin.WithContainerRuntimeArgs("--label", $"project={label}"))
    .WithContainerRuntimeArgs("--label", $"project={label}");

var db = postgres.AddDatabase("skoleplanen");
var keycloakDb = postgres.AddDatabase("keycloak");

// Keycloak (raw container — no first-party Aspire hosting package)
var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.2")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin")
    .WithEnvironment("KC_DB", "postgres")
    .WithEnvironment("KC_DB_USERNAME", postgres.Resource.UserNameParameter!)
    .WithEnvironment("KC_DB_PASSWORD", postgres.Resource.PasswordParameter)
    .WithEnvironment("KC_DB_URL", ReferenceExpression.Create(
        $"jdbc:postgresql://{postgres.Resource.Host}:{postgres.Resource.Port}/keycloak"))
    .WithContainerRuntimeArgs("--label", $"project={label}")
    .WithArgs("start-dev")
    .WaitFor(postgres);

// LocalStack (S3-compatible local emulation for OVHCloud Object Storage)
var localstack = builder.AddContainer("localstack", "localstack/localstack", "3")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(port: 4566, targetPort: 4566, name: "gateway")
    .WithContainerRuntimeArgs("--label", $"project={label}");

// API — will be wired up when the API project is created
// var api = builder.AddProject<Projects.Skoleplanen_Api>("api")
//     .WithReference(db)
//     .WithReference(keycloak)
//     .WaitFor(db)
//     .WaitFor(keycloak);

// React + Vite frontend — will be wired up when the web project is created
// var web = builder.AddNpmApp("web", workingDirectory: "../../../web", scriptName: "dev")
//     .WithHttpEndpoint(port: 5173, env: "PORT")
//     .WithExternalHttpEndpoints()
//     .WithReference(api);

builder.Build().Run();
