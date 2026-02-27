// General site-wide JavaScript functions

/**
 * Clicks an element (used for triggering file inputs)
 * @param {HTMLElement} element - The element to click
 */
window.clickElement = function (element) {
    if (element) {
        element.click();
    }
};

/**
 * Downloads a file from base64 encoded data
 * @param {string} fileName - The name of the file to download
 * @param {string} base64Data - The base64 encoded file content
 * @param {string} contentType - The MIME type of the file
 */
window.downloadFileFromBase64 = function (fileName, base64Data, contentType) {
    const byteCharacters = atob(base64Data);
    const byteNumbers = new Array(byteCharacters.length);
    for (let i = 0; i < byteCharacters.length; i++) {
        byteNumbers[i] = byteCharacters.charCodeAt(i);
    }
    const byteArray = new Uint8Array(byteNumbers);
    const blob = new Blob([byteArray], { type: contentType });
    
    const link = document.createElement('a');
    link.href = URL.createObjectURL(blob);
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(link.href);
};
