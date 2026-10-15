const contactForm = document.querySelector('#contact-form');

if (contactForm) {
	const fields = [...contactForm.querySelectorAll('[data-validate]')];
	const formStatus = document.querySelector('#contact-status');

	function validateField(field) {
		const error = document.querySelector(`#${field.id}-error`);
		let message = '';

		if (!field.value.trim()) {
			message = 'Vul dit veld in.';
		} else if (field.type === 'email' && !field.validity.valid) {
			message = 'Vul een geldig e-mailadres in.';
		} else if (field.name === 'name' && field.value.trim().length < 2) {
			message = 'Je naam moet minimaal 2 tekens bevatten.';
		} else if (field.name === 'message' && field.value.trim().length < 10) {
			message = 'Je bericht moet minimaal 10 tekens bevatten.';
		}

		field.setAttribute('aria-invalid', String(Boolean(message)));
		error.textContent = message;
		return !message;
	}

	fields.forEach((field) => {
		field.addEventListener('input', () => {
			validateField(field);
			formStatus.replaceChildren();
		});
	});

	contactForm.addEventListener('submit', (event) => {
		event.preventDefault();
		const valid = fields.map(validateField);
		const invalidField = fields[valid.indexOf(false)];

		if (invalidField) {
			invalidField.focus();
			formStatus.textContent = 'Controleer de gemarkeerde velden.';
			return;
		}

		const name = contactForm.elements.namedItem('name').value.trim();
		const email = contactForm.elements.namedItem('email').value.trim();
		const message = contactForm.elements.namedItem('message').value.trim();
		const subject = encodeURIComponent(`Bericht van ${name} via RLLY`);
		const body = encodeURIComponent(`${message}\n\nNaam: ${name}\nE-mailadres: ${email}`);
		const mailLink = document.createElement('a');
		mailLink.textContent = 'Open je e-mailprogramma om het bericht te versturen';
		mailLink.href = `mailto:raileyboer04@gmail.com?subject=${subject}&body=${body}`;
		const confirmation = document.createElement('span');
		confirmation.textContent = 'Je gegevens zijn gecontroleerd. ';
		formStatus.replaceChildren(confirmation, mailLink);
	});
}
