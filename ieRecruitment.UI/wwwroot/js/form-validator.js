function validateRequiredFields(containerSelector = '') {
    let isValid = true;
    debugger
    // Select all inputs/selects/textareas marked as required
    const $fields = $(`${containerSelector} input[required], ${containerSelector} select[required], ${containerSelector} textarea[required]`);

    $fields.each(function () {
        const $field = $(this);
        const value = $field.val().trim();
        const errorLabelId = $field.attr('id') + '-error';
        let $errorLabel = $('#' + errorLabelId);

        // Create error label if not exists
        if ($errorLabel.length === 0) {
            $errorLabel = $('<label class="error-message" id="' + errorLabelId + '">This value is required.</label>');
            $field.after($errorLabel);
        }

        if (!value) {
            $errorLabel.show();
            isValid = false;
        } else {
            $errorLabel.hide();
        }
    });

    return isValid;
}
function attachInputValidationListeners() {
    $('.input-group input').on('input', function () {
        const inputId = $(this).attr('id');
        const errorLabel = $(`#${inputId}-error`);
        if (errorLabel.length > 0) {
            errorLabel.text('').hide();
        }
    });
}
// Email validation regex (common pattern)
function isValidEmail(email) {
    const emailPattern = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailPattern.test(email.trim());
}

// Mobile number validation (10-digit)
function isValidMobile(mobile) {
    const mobilePattern = /^[0-9]{10}$/;
    return mobilePattern.test(mobile.trim());
}

// Optional: Combined identifier validation (Email or Mobile)
function isValidIdentifier(value) {
    return isValidEmail(value) || isValidMobile(value);
}
