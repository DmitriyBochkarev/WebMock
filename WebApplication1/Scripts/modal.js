// Глобальная переменная для хранения ID редактируемого мока
let currentMockId = null;

// Функция открытия модального окна для редактирования
function openEditModal(mockId) {
    currentMockId = mockId;
    const modal = document.getElementById('mockModal');
    fetch(`/api/mock/configurations/${mockId}`)
        .then(response => response.json())
        .then(mock => {
            // Заполняем форму данными мока
            document.getElementById('editMethod').value = mock.Method;
            document.getElementById('editPath').value = mock.Path;
            document.getElementById('editStatusCode').value = mock.Response.StatusCode;

            // Парсим тело ответа
            let responseBody = mock.Response.Body;
            if (typeof responseBody === 'string') {
                try {
                    responseBody = JSON.parse(responseBody);
                    responseBody = JSON.stringify(responseBody, null, 2);
                } catch (e) {
                    // Оставляем как есть
                }
            } else {
                responseBody = JSON.stringify(responseBody, null, 2);
            }
            document.getElementById('editResponseBody').value = responseBody;

            // Заполняем заголовки (заголовки заполняются функцией addHeaderWithValues)
            const headersDiv = document.getElementById('editHeaders');
            if (mock.Response.Headers) {
                Object.entries(mock.Response.Headers).forEach(([key, value]) => {
                    addHeaderWithValues(key, value);
                });
            }

            // Открываем модальное окно
            modal.style.display = 'block';
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Ошибка при загрузке мока');
        });
}

// Универсальная функция сохранения
function saveMock() {
    const id = currentMockId
    const method = document.getElementById('editMethod').value;
    const path = document.getElementById('editPath').value;
    const statusCode = parseInt(document.getElementById('editStatusCode').value);
    let responseBody = document.getElementById('editResponseBody').value;

    try {
        responseBody = JSON.parse(responseBody);
    } catch (e) {
        // Оставляем как строку
    }

    // Собираем заголовки
    const headers = {};
    const headerRows = document.getElementById('editHeaders').children;
    for (let row of headerRows) {
        const key = row.children[0].value;
        const value = row.children[1].value;
        if (key && value) {
            headers[key] = value;
        }
    }

    fetch(`api/mock/update/${id}`, {
        method: 'PUT',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify({
            Path: path,
            Method: method,
            Response: {
                StatusCode: statusCode,
                Body: responseBody,
                Headers: headers
            }
        })
    })
        .then(response => response.json())
        .then(data => {
            if (data.Success) {
                alert(data.Message);
                closeModal();
                refreshMocks();
            } else {
                alert('Ошибка: ' + (data.Message || 'Unknown error'));
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('Ошибка при сохранении мока');
        });
}

// Функция закрытия модального окна
function closeModal() {
    document.getElementById('mockModal').style.display = 'none';
    currentMockId = null;
}

// Закрытие по клику на крестик
document.querySelector('.close').addEventListener('click', closeModal);

// Закрытие по клику вне окна
window.addEventListener('click', function (event) {
    const modal = document.getElementById('mockModal');
    if (event.target == modal) {
        closeModal();
    }
});