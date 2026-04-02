const string label = "skoleplanen";

var builder = DistributedApplication.CreateBuilder(args);

// PostgreSQL — shared server, separate databases for API and Keycloak
var pgUsername = builder.AddParameter("postgres-username", "postgres");
var pgPassword = builder.AddParameter("postgres-password", secret: true);

var postgres = builder.AddPostgres("postgres", userName: pgUsername, password: pgPassword)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithPgAdmin(pgAdmin => pgAdmin.WithContainerRuntimeArgs("--label", $"com.docker.compose.project={label}"))
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={label}");

var db = postgres.AddDatabase("skoleplanen-db");
var keycloakDb = postgres.AddDatabase("keycloak-db");

// Keycloak (raw container — no first-party Aspire hosting package)
var keycloak = builder.AddContainer("keycloak", "quay.io/keycloak/keycloak", "26.2")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(port: 8080, targetPort: 8080, name: "http")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", "admin")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", "admin")
    .WithEnvironment("KC_DB", "postgres")
    .WithEnvironment("KC_DB_USERNAME", pgUsername)
    .WithEnvironment("KC_DB_PASSWORD", pgPassword)
    .WithEnvironment("KC_DB_URL", ReferenceExpression.Create(
        $"jdbc:postgresql://{postgres.Resource.Host}:{postgres.Resource.Port}/keycloak-db"))
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={label}")
    .WithArgs("start-dev")
    .WaitFor(postgres);

// LocalStack (S3-compatible local emulation for OVHCloud Object Storage)
var localstack = builder.AddContainer("localstack", "localstack/localstack", "3")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHttpEndpoint(port: 4566, targetPort: 4566, name: "gateway")
    .WithContainerRuntimeArgs("--label", $"com.docker.compose.project={label}");

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
