Here are some examples of configurable McpClients to be configured
in appsettings(.Development).json McpClientSettings



    "McpClientSettings": {
        "Clients": [
            {
                "Name": "SequentialThinking",
                "Command": "npx",
                "Arguments": ["-y", "@modelcontextprotocol/server-sequential-thinking"]
            }
        ]
    }