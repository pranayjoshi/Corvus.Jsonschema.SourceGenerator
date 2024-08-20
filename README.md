# Corvus.Jsonschema.SourceGenerator

## Overview

Corvus.Jsonschema.SourceGenerator is a .NET Core project designed to generate source code based on JSON schemas. It includes tools and utilities to facilitate the generation of strongly-typed classes from JSON schema definitions, making it easier to work with JSON data in a type-safe manner.

### Project Structure

- **ConsumeGenerator/**: Contains the consumer application for the source generator.
  - `Model/FlimFlam.cs`: Example model class.
  - `Program.cs`: Main entry point for the consumer application.
  - `test.json`: Sample JSON file.
- **Corvus.Jsonschema.SourceGenerator/**: Contains the source generator logic.
  - `IncrementalSourceGenerator.cs`: Core source generator implementation.
  - `metaschema/`: Directory containing meta-schema definitions.
  - `Metaschema.cs`: Meta-schema handling logic.
- **docs/**: Documentation files.
  - `ADR/`: Architectural Decision Records.
- **.idea/**: IDE-specific configuration files.
- **.gitignore**: Git ignore rules.
- **LICENSE**: License file.
- **README.md**: Project overview and setup instructions.

## Installation

To set up the project locally, follow these steps:

1. **Clone the repository**:
    ```sh
    git clone https://github.com/pranayjoshi/Corvus.Jsonschema.SourceGenerator.git
    cd Corvus.Jsonschema.SourceGenerator
    ```

2. **Build the project**:
    Navigate to the `Corvus.Jsonschema.SourceGenerator` directory and build the project using the .NET CLI:
    ```sh
    dotnet build
    ```

3. **Run the consumer application**:
    Navigate to the `ConsumeGenerator` directory and run the application:
    ```sh
    dotnet run
    ```

## Usage

1. **Define your JSON schema**:
    Place your JSON schema files in the appropriate directory (e.g., `ConsumeGenerator/test.json`).

2. **Run the source generator**:
    The source generator will automatically process the JSON schema files and generate the corresponding C# classes.

3. **Use the generated classes**:
    Import and use the generated classes in your application code.

## Contributing

Contributions are welcome! Please read the [contributing guidelines](docs/CONTRIBUTING.md) before submitting a pull request.

## License

This project is licensed under the terms of the [MIT License](LICENSE).