// Quantity buttons: change the number in the field, between data-min and data-max.
for (const stepper of document.querySelectorAll('.stepper')) {
    const input = stepper.querySelector('input');
    for (const button of stepper.querySelectorAll('button')) {
        button.addEventListener('click', () => {
            const value = Number(input.value) + Number(button.dataset.step);
            input.value = String(Math.min(Number(input.dataset.max), Math.max(Number(input.dataset.min), value)));
        });
    }
}
