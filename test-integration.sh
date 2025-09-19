#!/bin/bash

# Test script for MCP Server integration
echo "🚀 Testing MCP Server Integration"
echo "=================================="

BASE_URL="http://localhost:8080"

echo "1. Testing Health Check..."
curl -s "$BASE_URL/health" | jq '.' || echo "❌ Health check failed"
echo ""

echo "2. Testing Tools Endpoint..."
curl -s "$BASE_URL/tools" | jq '.tools[].name' || echo "❌ Tools endpoint failed"
echo ""

echo "3. Testing Server Info..."
curl -s "$BASE_URL/info" | jq '.server_name, .version' || echo "❌ Server info failed"
echo ""

echo "4. Testing Query Processing (General Assistant)..."
curl -s -X POST "$BASE_URL/query" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "Hello, how are you?",
    "tool": "general-assistant",
    "userId": "test-user"
  }' | jq '.success, .tool_used' || echo "❌ Query processing failed"
echo ""

echo "5. Testing Query Processing (Database Tool)..."
curl -s -X POST "$BASE_URL/query" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "Show me all users from the database",
    "tool": "database-tool",
    "userId": "test-user"
  }' | jq '.success, .tool_used' || echo "❌ Database query failed"
echo ""

echo "6. Testing Invalid Query..."
curl -s -X POST "$BASE_URL/query" \
  -H "Content-Type: application/json" \
  -d '{
    "query": "",
    "userId": "test-user"
  }' | jq '.success, .error' || echo "❌ Invalid query test failed"
echo ""

echo "7. Testing Tool Status..."
curl -s "$BASE_URL/tools/database-tool/status" | jq '.tool_name, .status' || echo "❌ Tool status failed"
echo ""

echo "✅ Integration tests completed!"
echo ""
echo "📝 Notes:"
echo "- Make sure MCPServer is running on port 8080"
echo "- Configure Claude API key for full functionality"
echo "- Check logs for detailed error information"