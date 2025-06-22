# Document Search Functionality

## Overview
Added comprehensive search functionality to help users find files by name in Azure Storage containers, especially useful when dealing with large numbers of files.

## Features Implemented

### Backend (.NET API)
1. **New API Endpoint**: `GET /api/storage/containers/{containerName}/documents/search`
   - Query parameters: `searchTerm` (required), `maxResults` (optional, default: 100, max: 1000)
   - Supports wildcard patterns using `*` and `?`
   - Case-insensitive matching
   - Efficient pagination to handle large containers

2. **Search Service Method**: `SearchDocumentsAsync`
   - Implements wildcard pattern matching using regex
   - Optimized to stop processing early when enough results are found
   - Comprehensive error handling and logging
   - Returns detailed search metadata (total matches, has more results, etc.)

3. **New Response Model**: `SearchDocumentsResponse`
   - Includes search term, result count, pagination info
   - Indicates if more results are available

### Frontend (HTML/JavaScript)
1. **Search Interface**
   - Search input with wildcard support hints
   - Search and clear buttons
   - Auto-shown when a container is selected
   - Enter key support for quick searching

2. **Search Results Display**
   - Highlighted search terms in results
   - Search metadata header showing match count
   - "Show all documents" option to clear search
   - Visual indicators for partial results

3. **Enhanced UX**
   - Smooth transitions and animations
   - Proper loading states
   - Error handling with user-friendly messages
   - Maintains document selection across searches

## Search Examples
- `*.pdf` - Find all PDF files
- `invoice*` - Find files starting with "invoice"
- `document?.txt` - Find files like "document1.txt", "documenta.txt"
- `report` - Find files containing "report" in the name

## Performance Optimizations
- Early termination when sufficient results are found
- Configurable result limits to prevent overwhelming the UI
- Efficient blob enumeration with minimal metadata
- Client-side search term highlighting

## Security Considerations
- Uses Azure Managed Identity for authentication
- Input validation and sanitization
- Rate limiting through Azure Storage built-in protections
- Proper error handling without exposing internal details

## Usage
1. Select a storage container
2. Enter a search term (supports wildcards)
3. Click search or press Enter
4. View results with highlighted matches
5. Click "Clear" to return to all documents
