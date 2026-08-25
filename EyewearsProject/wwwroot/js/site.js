// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function wireImageUpload(fileInputId, urlInputId, previewId, uploadUrl, token) {
    const fileInput = document.getElementById(fileInputId);
    const urlInput = document.getElementById(urlInputId);
    const preview = document.getElementById(previewId);
    if (!fileInput) return;

    // Status line so upload progress/errors are actually visible instead of failing silently.
    let status = document.getElementById(fileInputId + '-status');
    if (!status) {
        status = document.createElement('div');
        status.id = fileInputId + '-status';
        status.className = 'small mt-1';
        fileInput.insertAdjacentElement('afterend', status);
    }

    fileInput.addEventListener('change', async function () {
        const file = fileInput.files[0];
        if (!file) return;

        status.textContent = 'Uploading...';
        status.className = 'small mt-1 text-muted';

        const formData = new FormData();
        formData.append('file', file);
        formData.append('__RequestVerificationToken', token);

        try {
            const response = await fetch(uploadUrl, { method: 'POST', body: formData });

            if (!response.ok) {
                status.textContent = 'Upload failed (server error ' + response.status + '). Try logging out and back into Admin, then retry.';
                status.className = 'small mt-1 text-danger';
                return;
            }

            const data = await response.json();

            if (data.success) {
                urlInput.value = data.url;
                status.textContent = 'Uploaded: ' + file.name;
                status.className = 'small mt-1 text-success';
                if (preview) {
                    preview.src = data.url;
                    preview.style.display = 'block';
                }
            } else {
                status.textContent = data.message || 'Upload failed.';
                status.className = 'small mt-1 text-danger';
            }
        } catch (err) {
            status.textContent = 'Upload error: ' + err.message;
            status.className = 'small mt-1 text-danger';
        }
    });
}