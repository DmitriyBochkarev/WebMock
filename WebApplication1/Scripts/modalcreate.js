// Функция открытия модального окна для редактирования
function openCreateModal() {
    const modalCreate = document.getElementById('mockCreateModal');
    
            // Открываем модальное окно
            modalCreate.style.display = 'block';
     
}
// Функция закрытия модального окна
function closeCreateModal() {
    document.getElementById('mockCreateModal').style.display = 'none';
    currentMockId = null;
}