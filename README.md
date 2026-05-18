# DynamoDB AWS Manager

A WPF desktop application for managing AWS DynamoDB tables with full CRUD support — no AWS Console required.

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?style=flat-square)
![Platform](https://img.shields.io/badge/platform-Windows-0078D4?style=flat-square)
![AWS](https://img.shields.io/badge/AWS-DynamoDB-FF9900?style=flat-square)

---

## Features

- **Browse tables** — lists all DynamoDB tables in your AWS account
- **Create tables** — define partition key, optional sort key, and key types (String / Number)
- **Drop tables** — delete a table and all its data with confirmation
- **Scan items** — loads all items from any selected table with dynamic columns
- **Add items** — dialog with key fields pre-generated from the table schema, plus custom extra attributes
- **Edit items** — edit any item with pre-filled values
- **Delete items** — removes selected item by primary key
- **Search** — filters rows across all visible attributes in real time
- **Activity log** — timestamped log of every operation inside the app

---

## Tech Stack

| Layer | Technology |
|---|---|
| UI | WPF (.NET 8, C#) |
| AWS SDK | AWSSDK.DynamoDBv2 |
| Config | Microsoft.Extensions.Configuration.Json |

---

## Getting Started

### 1. Clone the repo

```bash
git clone https://github.com/Makarrco/DynamoAwsManager.git
cd DynamoAwsManager
```

### 2. Add your AWS credentials

Create `appsettings.json` in the project root (this file is gitignored — never commit it):

```json
{
  "AWS": {
    "AccessKey": "YOUR_ACCESS_KEY",
    "SecretKey": "YOUR_SECRET_KEY",
    "Region": "us-east-1"
  }
}
```

### 3. Run

```bash
dotnet run
```

Or open `DynamoAwsManager.sln` in Visual Studio and press **F5**.

---

## Project Structure

```
DynamoAwsManager/
├── MainWindow.xaml          # Main UI layout
├── MainWindow.xaml.cs       # Main window logic
├── DynamoDbService.cs       # All AWS DynamoDB API calls
├── DynamicItem.cs           # Generic row model for any table
├── CreateTableDialog.xaml   # Dialog — create new table
├── CreateTableDialog.xaml.cs
├── AddItemDialog.xaml       # Dialog — add / edit item
├── AddItemDialog.xaml.cs
├── appsettings.json         # AWS credentials (gitignored)
└── DynamoAwsManager.csproj
```

---

## IAM Permissions Required

Your AWS user needs the following DynamoDB permissions:

```json
{
  "Effect": "Allow",
  "Action": [
    "dynamodb:ListTables",
    "dynamodb:DescribeTable",
    "dynamodb:CreateTable",
    "dynamodb:DeleteTable",
    "dynamodb:Scan",
    "dynamodb:PutItem",
    "dynamodb:DeleteItem"
  ],
  "Resource": "*"
}
```

---

## Security

> **Never commit `appsettings.json`.**  
> It is listed in `.gitignore` by default. Use IAM roles with least-privilege access and rotate your keys regularly.
