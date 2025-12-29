## 📂 Data & Knowledge Base

The bot relies on a specific dataset to answer questions. The source of truth for this data is located in the `/SampleData` folder.

### 📄 Source Data File
* **Location:** [`SampleData`](https://github.com/Koushiksai2127/Azure-AI-Assistant/blob/main/SampleData/Data_Format)
* **Format:** JSON Array
* **Content:** Contains guides, links and scope definitions.

### 🔄 How to Update the Index
If you modify `Data_Format.json`, you must update the Azure Search Index:

1.  **Upload:** Upload the updated JSON file to your Azure Storage Account container (`documents`).
2.  **Re-run Indexer:**
    * Go to Azure Portal > **Azure AI Search**.
    * Select **Indexers** > Click your indexer (e.g., `my-indexer`).
    * Click **Run**.
3.  **Verify:** Use the **Search Explorer** in the portal to confirm the new ID/Topics are searchable.
