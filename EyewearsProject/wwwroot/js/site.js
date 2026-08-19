// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function wireImageUpload(fileInputId, urlInputId, previewId, uploadUrl, token) {
    const fileInput = document.getElementById(fileInputId);
    const urlInput = document.getElementById(urlInputId);
    const preview = document.getElementById(previewId);

    if (!fileInput) return;

    fileInput.addEventListener('change', async function () {
        const file = fileInput.files[0];
        if (!file) return;

        const formData = new FormData();
        formData.append('file', file);
        formData.append('__RequestVerificationToken', token);

        const response = await fetch(uploadUrl, { method: 'POST', body: formData });
        const data = await response.json();

        if (data.success) {
            urlInput.value = data.url;
            if (preview) {
                preview.src = data.url;
                preview.style.display = 'block';
            }
        } else {
            alert(data.message);
        }
    });
}