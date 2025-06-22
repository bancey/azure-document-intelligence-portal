#!/bin/bash

# Test runner script for Document Intelligence Portal
# This script provides various test execution options

set -e  # Exit on any error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Print colored output
print_colored() {
    printf "${2}${1}${NC}\n"
}

# Print usage information
usage() {
    echo "Usage: $0 [OPTIONS]"
    echo ""
    echo "Options:"
    echo "  -a, --all           Run all tests"
    echo "  -u, --unit          Run unit tests only"
    echo "  -i, --integration   Run integration tests only"
    echo "  -c, --coverage      Run tests with code coverage"
    echo "  -w, --watch         Run tests in watch mode"
    echo "  -f, --filter FILTER Run tests matching filter"
    echo "  -r, --report        Generate coverage report (requires --coverage)"
    echo "  -v, --verbose       Verbose output"
    echo "  -h, --help          Show this help message"
    echo ""
    echo "Examples:"
    echo "  $0 --unit                    # Run unit tests"
    echo "  $0 --coverage --report       # Run tests with coverage and generate report"
    echo "  $0 --filter \"Storage\"        # Run tests with 'Storage' in the name"
    echo "  $0 --integration             # Run integration tests"
}

# Default values
RUN_ALL=false
RUN_UNIT=false
RUN_INTEGRATION=false
COVERAGE=false
WATCH=false
GENERATE_REPORT=false
VERBOSE=false
FILTER=""

# Parse command line arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        -a|--all)
            RUN_ALL=true
            shift
            ;;
        -u|--unit)
            RUN_UNIT=true
            shift
            ;;
        -i|--integration)
            RUN_INTEGRATION=true
            shift
            ;;
        -c|--coverage)
            COVERAGE=true
            shift
            ;;
        -w|--watch)
            WATCH=true
            shift
            ;;
        -f|--filter)
            FILTER="$2"
            shift 2
            ;;
        -r|--report)
            GENERATE_REPORT=true
            shift
            ;;
        -v|--verbose)
            VERBOSE=true
            shift
            ;;
        -h|--help)
            usage
            exit 0
            ;;
        *)
            print_colored "Unknown option: $1" $RED
            usage
            exit 1
            ;;
    esac
done

# If no specific test type is selected, run all
if [[ "$RUN_UNIT" == false && "$RUN_INTEGRATION" == false && "$RUN_ALL" == false && -z "$FILTER" ]]; then
    RUN_ALL=true
fi

# Check if .NET is installed
if ! command -v dotnet &> /dev/null; then
    print_colored "Error: .NET SDK is not installed or not in PATH" $RED
    exit 1
fi

# Check .NET version
DOTNET_VERSION=$(dotnet --version)
print_colored "Using .NET version: $DOTNET_VERSION" $BLUE

# Navigate to the test project directory
TEST_DIR="$(dirname "$0")"
cd "$TEST_DIR"

print_colored "Document Intelligence Portal - Test Runner" $BLUE
print_colored "==========================================" $BLUE

# Build the project first
print_colored "Building test project..." $YELLOW
if ! dotnet build --configuration Debug; then
    print_colored "Build failed!" $RED
    exit 1
fi

# Prepare test command
TEST_CMD="dotnet test"

# Add verbosity if requested
if [[ "$VERBOSE" == true ]]; then
    TEST_CMD="$TEST_CMD --verbosity detailed"
fi

# Add coverage collection
if [[ "$COVERAGE" == true ]]; then
    TEST_CMD="$TEST_CMD --collect:\"XPlat Code Coverage\""
    print_colored "Code coverage collection enabled" $YELLOW
fi

# Add filter based on test type or custom filter
if [[ -n "$FILTER" ]]; then
    TEST_CMD="$TEST_CMD --filter \"$FILTER\""
    print_colored "Running tests with filter: $FILTER" $YELLOW
elif [[ "$RUN_UNIT" == true ]]; then
    # Filter out integration tests that might require Azure services
    TEST_CMD="$TEST_CMD --filter \"FullyQualifiedName!~Integration&FullyQualifiedName!~EndToEnd\""
    print_colored "Running unit tests only" $YELLOW
elif [[ "$RUN_INTEGRATION" == true ]]; then
    TEST_CMD="$TEST_CMD --filter \"FullyQualifiedName~Integration|FullyQualifiedName~EndToEnd\""
    print_colored "Running integration tests only" $YELLOW
    print_colored "Note: Some integration tests may be skipped if Azure services are not configured" $YELLOW
else
    print_colored "Running all tests" $YELLOW
fi

# Add watch mode
if [[ "$WATCH" == true ]]; then
    TEST_CMD="dotnet watch test"
    if [[ "$VERBOSE" == true ]]; then
        TEST_CMD="$TEST_CMD -- --verbosity detailed"
    fi
    print_colored "Starting tests in watch mode..." $YELLOW
    print_colored "Press Ctrl+C to stop" $YELLOW
fi

# Run the tests
print_colored "Executing: $TEST_CMD" $BLUE
if eval $TEST_CMD; then
    print_colored "Tests completed successfully!" $GREEN
    TEST_SUCCESS=true
else
    print_colored "Some tests failed!" $RED
    TEST_SUCCESS=false
fi

# Generate coverage report if requested
if [[ "$COVERAGE" == true && "$GENERATE_REPORT" == true && "$TEST_SUCCESS" == true ]]; then
    print_colored "Generating coverage report..." $YELLOW
    
    # Check if reportgenerator is installed
    if ! command -v reportgenerator &> /dev/null; then
        print_colored "Installing ReportGenerator..." $YELLOW
        dotnet tool install -g dotnet-reportgenerator-globaltool
    fi
    
    # Generate the report
    COVERAGE_DIR="TestResults/CoverageReport"
    mkdir -p "$COVERAGE_DIR"
    
    if reportgenerator -reports:**/coverage.cobertura.xml -targetdir:"$COVERAGE_DIR" -reporttypes:Html; then
        print_colored "Coverage report generated at: $COVERAGE_DIR/index.html" $GREEN
        
        # Try to open the report in the default browser
        if command -v xdg-open &> /dev/null; then
            xdg-open "$COVERAGE_DIR/index.html" 2>/dev/null || true
        elif command -v open &> /dev/null; then
            open "$COVERAGE_DIR/index.html" 2>/dev/null || true
        fi
    else
        print_colored "Failed to generate coverage report" $RED
    fi
fi

# Print summary
print_colored "=========================================" $BLUE
if [[ "$TEST_SUCCESS" == true ]]; then
    print_colored "✅ Test execution completed successfully" $GREEN
else
    print_colored "❌ Test execution completed with failures" $RED
    exit 1
fi

# Print helpful information
print_colored "Helpful commands:" $BLUE
echo "  Run unit tests:        $0 --unit"
echo "  Run with coverage:     $0 --coverage --report"
echo "  Watch mode:            $0 --watch"
echo "  Filter tests:          $0 --filter \"TestName\""
echo "  Verbose output:        $0 --verbose"
