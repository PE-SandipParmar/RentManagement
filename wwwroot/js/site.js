// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
$(document).ready(function () {
    // Initialize tooltips
    var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });

    // Auto-hide alerts after 5 seconds
    $('.alert').each(function () {
        var alert = $(this);
        if (alert.hasClass('alert-success') || alert.hasClass('alert-info')) {
            setTimeout(function () {
                alert.fadeOut();
            }, 5000);
        }
    });

    // Confirm delete actions
    $('.btn-delete').on('click', function (e) {
        e.preventDefault();
        var message = $(this).data('confirm') || 'Are you sure you want to delete this item?';
        if (confirm(message)) {
            $(this).closest('form').submit();
        }
    });

    // Password strength indicator
    $('#Password, #NewPassword').on('input', function () {
        var password = $(this).val();
        var strength = getPasswordStrength(password);
        updatePasswordStrengthIndicator($(this), strength);
    });

    // Character counter for text inputs
    $('.form-control[maxlength]').each(function () {
        var input = $(this);
        var maxLength = input.attr('maxlength');
        var counter = $('<small class="form-text text-muted char-counter">' + input.val().length + '/' + maxLength + '</small>');
        input.after(counter);

        input.on('input', function () {
            counter.text(input.val().length + '/' + maxLength);
            if (input.val().length > maxLength * 0.9) {
                counter.addClass('text-warning');
            } else {
                counter.removeClass('text-warning');
            }
        });
    });

    // Real-time validation feedback
    $('.form-control').on('blur', function () {
        validateField($(this));
    });
});

function getPasswordStrength(password) {
    var strength = 0;
    if (password.length >= 8) strength++;
    if (/[a-z]/.test(password)) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    if (/[^A-Za-z0-9]/.test(password)) strength++;
    return strength;
}

function updatePasswordStrengthIndicator(input, strength) {
    var container = input.closest('.form-group');
    var indicator = container.find('.password-strength');

    if (indicator.length === 0) {
        indicator = $('<div class="password-strength mt-1"></div>');
        input.after(indicator);
    }

    var strengthText = ['Very Weak', 'Weak', 'Fair', 'Good', 'Strong'];
    var strengthClass = ['danger', 'danger', 'warning', 'info', 'success'];

    if (input.val().length === 0) {
        indicator.hide();
        return;
    }

    indicator.show();
    indicator.html('<small class="text-' + strengthClass[Math.max(0, strength - 1)] + '">Password Strength: ' + strengthText[Math.max(0, strength - 1)] + '</small>');
}

function validateField(field) {
    var value = field.val();
    var isValid = true;
    var message = '';

    // Email validation
    if (field.attr('type') === 'email' && value) {
        var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
        if (!emailRegex.test(value)) {
            isValid = false;
            message = 'Please enter a valid email address';
        }
    }

    // Phone validation
    if (field.attr('name') === 'PhoneNumber' && value) {
        var phoneRegex = /^[\+]?[1-9][\d]{0,15}$/;
        if (!phoneRegex.test(value.replace(/[\s\-\(\)]/g, ''))) {
            isValid = false;
            message = 'Please enter a valid phone number';
        }
    }

    // Update field styling
    if (isValid) {
        field.removeClass('is-invalid').addClass('is-valid');
        field.siblings('.invalid-feedback').hide();
    } else {
        field.removeClass('is-valid').addClass('is-invalid');
        var feedback = field.siblings('.invalid-feedback');
        if (feedback.length === 0) {
            feedback = $('<div class="invalid-feedback"></div>');
            field.after(feedback);
        }
        feedback.text(message).show();
    }
}

// Utility functions
function showLoading(button) {
    button.prop('disabled', true);
    var originalText = button.text();
    button.data('original-text', originalText);
    button.html('<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Loading...');
}

function hideLoading(button) {
    button.prop('disabled', false);
    button.text(button.data('original-text'));
}

function showToast(message, type = 'success') {
    var toast = $('<div class="toast align-items-center text-white bg-' + type + ' border-0" role="alert">' +
        '<div class="d-flex">' +
        '<div class="toast-body">' + message + '</div>' +
        '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>' +
        '</div></div>');

    $('#toast-container').append(toast);
    var bsToast = new bootstrap.Toast(toast[0]);
    bsToast.show();

    toast.on('hidden.bs.toast', function () {
        toast.remove();
    });
}

// Add toast container to body if it doesn't exist
if ($('#toast-container').length === 0) {
    $('body').append('<div id="toast-container" class="toast-container position-fixed top-0 end-0 p-3"></div>');
}
* /