// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Sütun güncelleme fonksiyonu
async function updateColumn(tableName, oldColumnName) {
    const newColumnName = prompt("Enter new column name:", oldColumnName);
    if (!newColumnName) return;

    const newDataType = prompt("Enter new data type (e.g., VARCHAR(50), INT, etc.):");
    if (!newDataType) return;

    try {
        const response = await fetch('/DynamicTable/UpdateColumn', {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                tableName: tableName,
                oldColumnName: oldColumnName,
                newColumnName: newColumnName,
                newDataType: newDataType
            })
        });

        if (response.ok) {
            const result = await response.json();
            alert(result.message);
            location.reload();
        } else {
            const error = await response.text();
            alert('Error updating column: ' + error);
        }
    } catch (error) {
        alert('Error: ' + error);
    }
}

// Sütun silme fonksiyonu
async function deleteColumn(tableName, columnName) {
    if (!confirm(`Are you sure you want to delete column "${columnName}" from table "${tableName}"?`)) {
        return;
    }

    try {
        const response = await fetch(`/DynamicTable/DeleteColumn?tableName=${tableName}&columnName=${columnName}`, {
            method: 'DELETE'
        });

        if (response.ok) {
            alert('Column deleted successfully!');
            location.reload();
        } else {
            const error = await response.text();
            alert('Error deleting column: ' + error);
        }
    } catch (error) {
        alert('Error: ' + error);
    }
}
