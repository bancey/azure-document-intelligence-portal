// Document Intelligence Portal JavaScript

let selectedDocument = null;

// Initialize the application
document.addEventListener('DOMContentLoaded', function() {
    loadContainers();
    setupEventListeners();
});

// Set up event listeners
function setupEventListeners() {
    const containerSelect = document.getElementById('containerSelect');
    containerSelect.addEventListener('change', function() {
        if (this.value) {
            loadDocuments(this.value);
            // Show search section when container is selected
            document.getElementById('searchSection').style.display = 'block';
        } else {
            clearDocumentList();
            // Hide search section when no container is selected
            document.getElementById('searchSection').style.display = 'none';
            clearSearch();
        }
    });
    
    const searchInput = document.getElementById('searchInput');
    searchInput.addEventListener('keypress', function(event) {
        if (event.key === 'Enter') {
            searchDocuments();
        }
    });
}

// Load available storage containers
async function loadContainers() {
    try {
        showLoading('Loading containers...');
        const response = await fetch('/api/storage/containers');
        const data = await response.json();
        
        const containerSelect = document.getElementById('containerSelect');
        containerSelect.innerHTML = '<option value="">Select a container...</option>';
        
        if (data.success && data.containers) {
            data.containers.forEach(container => {
                const option = document.createElement('option');
                option.value = container;
                option.textContent = container;
                containerSelect.appendChild(option);
            });
            hideLoading();
        } else {
            showError('Failed to load containers: ' + (data.errorMessage || 'Unknown error'));
        }
    } catch (error) {
        console.error('Error loading containers:', error);
        showError('Failed to load containers. Please check your configuration.');
    }
}

// Load documents from selected container
async function loadDocuments(containerName) {
    try {
        showLoading('Loading documents...');
        const response = await fetch(`/api/storage/containers/${encodeURIComponent(containerName)}/documents`);
        const data = await response.json();
        
        const documentList = document.getElementById('documentList');
        
        if (data.success && data.documents) {
            if (data.documents.length === 0) {
                documentList.innerHTML = `
                    <div class="text-muted text-center">
                        <i class="fas fa-inbox me-2"></i>No documents found in this container
                    </div>
                `;
            } else {
                documentList.innerHTML = data.documents.map(doc => `
                    <div class="document-item" onclick="selectDocument('${containerName}', '${doc.name}', '${doc.blobUri}')">
                        <div class="d-flex align-items-center">
                            <i class="fas ${getFileIcon(doc.name)} me-3 text-primary"></i>
                            <div class="flex-grow-1">
                                <div class="fw-bold">${doc.name}</div>
                                <small class="text-muted">
                                    ${formatFileSize(doc.size)} • ${new Date(doc.lastModified).toLocaleDateString()}
                                </small>
                            </div>
                        </div>
                    </div>
                `).join('');
            }
            hideLoading();
        } else {
            showError('Failed to load documents: ' + (data.errorMessage || 'Unknown error'));
        }
    } catch (error) {
        console.error('Error loading documents:', error);
        showError('Failed to load documents from container.');
    }
}

// Select a document for analysis
function selectDocument(containerName, blobName, blobUri) {
    // Remove previous selection
    document.querySelectorAll('.document-item').forEach(item => {
        item.classList.remove('selected');
    });
    
    // Add selection to clicked item
    event.currentTarget.classList.add('selected');
    
    selectedDocument = {
        containerName: containerName,
        blobName: blobName,
        blobUri: blobUri
    };
    
    // Enable analyze button
    document.getElementById('analyzeBtn').disabled = false;
    
    console.log('Selected document:', selectedDocument);
}

// Analyze the selected document using the new streaming API
async function analyzeDocument() {
    if (!selectedDocument) {
        showError('Please select a document first.');
        return;
    }
    
    const modelId = document.getElementById('modelSelect').value;
    const analyzeBtn = document.getElementById('analyzeBtn');
    
    try {
        // Show loading state
        analyzeBtn.disabled = true;
        analyzeBtn.innerHTML = '<i class="fas fa-spinner fa-spin me-2"></i>Analyzing...';
        
        showAnalysisLoading();
        
        // Use the new streaming API endpoint
        const requestBody = {
            containerName: selectedDocument.containerName,
            blobName: selectedDocument.blobName,
            modelId: modelId,
            includeFieldElements: true
        };
        
        console.log('Analyzing document with streaming API:', requestBody);
        
        // Make API call to the new streaming endpoint
        const response = await fetch('/api/documentanalysis/analyze/stream', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestBody)
        });
        
        const data = await response.json();
        
        if (data.success && data.result) {
            showAnalysisResults(data.result, true); // Pass true to indicate streaming was used
            console.log('Analysis completed successfully using streaming API');
        } else {
            showAnalysisError('Analysis failed: ' + (data.message || 'Unknown error'));
        }
        
    } catch (error) {
        console.error('Error analyzing document:', error);
        showAnalysisError('Failed to analyze document. Please try again.');
    } finally {
        // Reset button state
        analyzeBtn.disabled = false;
        analyzeBtn.innerHTML = '<i class="fas fa-stream me-2"></i>Analyze with Streaming';
    }
}

// Show analysis results
function showAnalysisResults(result, usingStreaming = false) {
    const resultsDiv = document.getElementById('analysisResults');
    
    resultsDiv.innerHTML = `
        ${usingStreaming ? `
        <div class="alert alert-success mb-4" role="alert">
            <i class="fas fa-check-circle me-2"></i>
            <strong>Analysis Complete!</strong> Document processed using secure streaming API (no SAS tokens required).
        </div>
        ` : ''}
        
        <div class="analysis-summary mb-4">
            <h6 class="fw-bold mb-3">Analysis Summary</h6>
            <div class="row g-3">
                <div class="col-md-6">
                    <div class="bg-light p-3 rounded">
                        <small class="text-muted">Model Used</small>
                        <div class="fw-bold">${result.modelId}</div>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="bg-light p-3 rounded">
                        <small class="text-muted">Pages</small>
                        <div class="fw-bold">${result.pages.length}</div>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="bg-light p-3 rounded">
                        <small class="text-muted">Tables</small>
                        <div class="fw-bold">${result.tables.length}</div>
                    </div>
                </div>
                <div class="col-md-6">
                    <div class="bg-light p-3 rounded">
                        <small class="text-muted">Key-Value Pairs</small>
                        <div class="fw-bold">${result.keyValuePairs.length}</div>
                    </div>
                </div>
            </div>
        </div>

        <div class="mb-4">
            <h6 class="fw-bold mb-3">Extracted Content</h6>
            <div class="border rounded p-3" style="max-height: 200px; overflow-y: auto; background: #f8f9fa;">
                <pre class="mb-0" style="white-space: pre-wrap; font-size: 0.9rem;">${result.content || 'No content extracted'}</pre>
            </div>
        </div>

        ${result.keyValuePairs.length > 0 ? `
        <div class="mb-4">
            <h6 class="fw-bold mb-3">Key-Value Pairs</h6>
            <div class="table-responsive">
                <table class="table table-sm">
                    <thead>
                        <tr>
                            <th>Key</th>
                            <th>Value</th>
                            <th>Confidence</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${result.keyValuePairs.map(kvp => `
                            <tr>
                                <td class="fw-bold">${kvp.key}</td>
                                <td>${kvp.value}</td>
                                <td><span class="badge bg-${getConfidenceBadgeColor(kvp.confidence)}">${(kvp.confidence * 100).toFixed(1)}%</span></td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        </div>
        ` : ''}

        ${result.tables.length > 0 ? `
        <div class="mb-4">
            <h6 class="fw-bold mb-3">Tables (${result.tables.length})</h6>
            <div class="accordion" id="tablesAccordion">
                ${result.tables.map((table, index) => `
                    <div class="accordion-item">
                        <h2 class="accordion-header" id="table${index}">
                            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#tableCollapse${index}">
                                Table ${index + 1} (${table.rowCount} rows × ${table.columnCount} columns)
                            </button>
                        </h2>
                        <div id="tableCollapse${index}" class="accordion-collapse collapse" data-bs-parent="#tablesAccordion">
                            <div class="accordion-body">
                                ${renderTable(table)}
                            </div>
                        </div>
                    </div>
                `).join('')}
            </div>
        </div>
        ` : ''}

        <div class="mb-4">
            <h6 class="fw-bold mb-3">Raw Analysis Data</h6>
            <div class="json-viewer">
                <pre>${JSON.stringify(result, null, 2)}</pre>
            </div>
        </div>
    `;
}

// Render a table from analysis results
function renderTable(table) {
    if (!table.cells || table.cells.length === 0) {
        return '<p class="text-muted">No table data available</p>';
    }

    // Create a 2D array to represent the table
    const grid = Array(table.rowCount).fill(null).map(() => Array(table.columnCount).fill(''));
    
    // Fill the grid with cell content
    table.cells.forEach(cell => {
        if (cell.rowIndex < table.rowCount && cell.columnIndex < table.columnCount) {
            grid[cell.rowIndex][cell.columnIndex] = cell.content;
        }
    });

    // Generate HTML table
    let html = '<table class="table table-bordered table-sm">';
    grid.forEach((row, rowIndex) => {
        html += '<tr>';
        row.forEach((cell, colIndex) => {
            const tag = rowIndex === 0 ? 'th' : 'td';
            html += `<${tag} class="${rowIndex === 0 ? 'bg-light' : ''}">${cell || ''}</${tag}>`;
        });
        html += '</tr>';
    });
    html += '</table>';
    
    return html;
}

// Search documents in the selected container
async function searchDocuments() {
    const containerSelect = document.getElementById('containerSelect');
    const searchInput = document.getElementById('searchInput');
    const containerName = containerSelect.value;
    const searchTerm = searchInput.value.trim();
    
    if (!containerName) {
        showError('Please select a container first.');
        return;
    }
    
    if (!searchTerm) {
        showError('Please enter a search term.');
        return;
    }
    
    try {
        showLoading('Searching documents...');
        
        const response = await fetch(
            `/api/storage/containers/${encodeURIComponent(containerName)}/documents/search?` + 
            new URLSearchParams({
                searchTerm: searchTerm,
                maxResults: 100
            })
        );
        
        const data = await response.json();
        
        if (data.success) {
            displaySearchResults(data, containerName);
            document.getElementById('clearSearchBtn').style.display = 'inline-block';
        } else {
            showError('Search failed: ' + (data.errorMessage || 'Unknown error'));
        }
        
        hideLoading();
    } catch (error) {
        console.error('Error searching documents:', error);
        showError('Failed to search documents.');
        hideLoading();
    }
}

// Display search results
function displaySearchResults(data, containerName) {
    const documentList = document.getElementById('documentList');
    
    if (data.documents.length === 0) {
        documentList.innerHTML = `
            <div class="text-muted text-center">
                <i class="fas fa-search me-2"></i>No documents found matching "${data.searchTerm}"
                <div class="mt-2">
                    <button class="btn btn-sm btn-outline-primary" onclick="clearSearch()">
                        Show all documents
                    </button>
                </div>
            </div>
        `;
        return;
    }
    
    let resultsHtml = '';
    
    // Add search info header
    resultsHtml += `
        <div class="search-results-header mb-3 p-2 bg-light rounded">
            <div class="d-flex justify-content-between align-items-center">
                <span class="fw-bold text-primary">
                    <i class="fas fa-search me-1"></i>
                    Search Results for "${data.searchTerm}"
                </span>
                <span class="badge bg-primary">${data.documents.length} of ${data.totalMatches}</span>
            </div>
            ${data.hasMoreResults ? 
                '<small class="text-muted">Some results may not be shown. Try a more specific search term.</small>' : 
                ''
            }
        </div>
    `;
    
    // Add document results
    resultsHtml += data.documents.map(doc => `
        <div class="document-item" onclick="selectDocument('${containerName}', '${doc.name}', '${doc.blobUri}')">
            <div class="d-flex align-items-center">
                <i class="fas ${getFileIcon(doc.name)} me-3 text-primary"></i>
                <div class="flex-grow-1">
                    <div class="fw-bold">${highlightSearchTerm(doc.name, data.searchTerm)}</div>
                    <small class="text-muted">
                        ${formatFileSize(doc.size)} • ${new Date(doc.lastModified).toLocaleDateString()}
                    </small>
                </div>
            </div>
        </div>
    `).join('');
    
    documentList.innerHTML = resultsHtml;
}

// Highlight search terms in document names
function highlightSearchTerm(text, searchTerm) {
    if (!searchTerm || searchTerm.includes('*') || searchTerm.includes('?')) {
        // Don't highlight wildcards as they're patterns, not literal text
        return text;
    }
    
    const regex = new RegExp(`(${escapeRegExp(searchTerm)})`, 'gi');
    return text.replace(regex, '<mark>$1</mark>');
}

// Escape special regex characters
function escapeRegExp(string) {
    return string.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

// Clear search and show all documents
function clearSearch() {
    const containerSelect = document.getElementById('containerSelect');
    const searchInput = document.getElementById('searchInput');
    
    searchInput.value = '';
    document.getElementById('clearSearchBtn').style.display = 'none';
    
    if (containerSelect.value) {
        loadDocuments(containerSelect.value);
    }
}

// Helper functions
function clearDocumentList() {
    document.getElementById('documentList').innerHTML = `
        <div class="text-muted text-center">
            <i class="fas fa-info-circle me-2"></i>Select a container to view documents
        </div>
    `;
    selectedDocument = null;
    document.getElementById('analyzeBtn').disabled = true;
}

function showLoading(message = 'Loading...') {
    // You can implement a loading overlay here if needed
    console.log(message);
}

function hideLoading() {
    // Hide loading overlay
    console.log('Loading completed');
}

function showError(message) {
    console.error(message);
    // You can implement a toast notification here
    alert(message);
}

function showAnalysisLoading() {
    const resultsDiv = document.getElementById('analysisResults');
    resultsDiv.innerHTML = `
        <div class="text-center py-5">
            <div class="loading-spinner"></div>
            <p class="mt-3">Analyzing document with Azure Document Intelligence...</p>
            <small class="text-muted">This may take a few moments depending on document complexity.</small>
        </div>
    `;
}

function showAnalysisError(message) {
    const resultsDiv = document.getElementById('analysisResults');
    resultsDiv.innerHTML = `
        <div class="alert alert-danger">
            <i class="fas fa-exclamation-triangle me-2"></i>
            ${message}
        </div>
    `;
}

function getFileIcon(filename) {
    const extension = filename.split('.').pop().toLowerCase();
    switch (extension) {
        case 'pdf': return 'fa-file-pdf';
        case 'doc':
        case 'docx': return 'fa-file-word';
        case 'xls':
        case 'xlsx': return 'fa-file-excel';
        case 'jpg':
        case 'jpeg':
        case 'png':
        case 'gif':
        case 'bmp':
        case 'tiff': return 'fa-file-image';
        case 'txt': return 'fa-file-alt';
        default: return 'fa-file';
    }
}

function formatFileSize(bytes) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

function getConfidenceBadgeColor(confidence) {
    if (confidence >= 0.8) return 'success';
    if (confidence >= 0.6) return 'warning';
    return 'danger';
}
