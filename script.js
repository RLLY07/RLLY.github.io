const projects = [
	{
		number: '01',
		category: 'Webapplicatie',
		type: 'Webapplicatie · 2025',
		title: 'Klascompas',
		description: 'Een rustige digitale plek waar leerlingen en docenten overzicht houden op alles wat een schooldag vraagt.',
		tags: ['Research', 'UX/UI', 'Frontend', 'Backend'],
		image: 'images/kclogo.png',
		imageAlt: 'Logo van Klascompas',
		shape: 'shape-blue',
		task: 'Ontwerp een oplossing die hulpvragen in de klas inzichtelijk maakt, zodat leerlingen niet onnodig hoeven te wachten en leraren hun aandacht eerlijker en efficiënter kunnen verdelen.',
		role: 'Onderzoek, interfaceontwerp, front-end en back-end development.',
		link: 'project-klascompas.html',
		linkText: 'Open volledige case'
	},
	{
		number: '02',
		category: 'Desktop app',
		type: 'Desktop app · 2026',
		title: 'The Hague Heights',
		description: 'Een simulatieomgeving voor het testen van verschillende hotel layouts en risicoscenario’s.',
		tags: ['Simulation', 'UX/UI', 'Java'],
		image: 'images/The_Hague_Heights_logo.png',
		imageAlt: 'Logo van The Hague Heights',
		shape: 'shape-deep',
		task: 'Ontwerp een desktop applicatie die verschillende hotel layouts kan simuleren en scenario’s met risico’s inzichtelijk maakt.',
		role: 'Concept, interface-ontwerp en productdenken.',
		link: 'project-hotelsym.html',
		linkText: 'Open volledige case'
	},
	{
		number: '03',
		category: 'In ontwikkeling',
		type: 'W.I.P. · N/A',
		title: 'Nieuwe opdracht',
		description: 'Er staat nog geen afgeronde case hier. Zodra een nieuwe opdracht beschikbaar is, verschijnt deze plek hier.',
		tags: ['Coming soon', 'W.I.P.'],
		image: 'images/WIP.png',
		imageAlt: 'Work in progress project',
		shape: 'shape-deep',
		task: 'Deze plek is tijdelijk gereserveerd voor een nieuwe case die binnenkort wordt toegevoegd.',
		role: 'Werk in uitvoering. Er komt hier later een nieuwe opdracht te staan.',
		link: 'project-onderweg.html',
		linkText: 'Open W.I.P. pagina'
	}
];

function makeElement(tag, className, text) {
	const element = document.createElement(tag);
	if (className) element.className = className;
	if (text) element.textContent = text;
	return element;
}

function createProjectCard(project) {
	const item = makeElement('li', 'project-item');
	if (project.number === '01') item.classList.add('project-featured');
	const summary = makeElement('div', 'project-summary');
	summary.append(makeElement('span', 'project-number', project.number));

	const info = makeElement('article', 'project-info');
	info.append(
		makeElement('p', 'project-type', project.type),
		makeElement('h2', '', project.title),
		makeElement('p', '', project.description)
	);

	const tags = makeElement('footer', 'tag-list');
	project.tags.forEach((tag) => tags.append(makeElement('span', '', tag)));
	info.append(tags);

	const figure = makeElement('figure', `project-shape ${project.shape}`);
	figure.setAttribute('aria-hidden', 'true');
	const image = makeElement('img', 'project-photo');
	image.src = project.image;
	image.alt = '';
	if (project.category === 'Desktop app' || project.category === 'In ontwikkeling') {
		image.classList.add('project-logo');
	}
	figure.append(image);

	const toggle = makeElement('button', 'project-toggle');
	toggle.type = 'button';
	toggle.setAttribute('aria-expanded', 'false');
	toggle.setAttribute('aria-label', `Toon projectdetails voor ${project.title}`);
	toggle.append(makeElement('span', '', '+'));
	toggle.firstElementChild.setAttribute('aria-hidden', 'true');

	const projectDetails = makeElement('section', 'project-details');
	projectDetails.id = `project-details-${project.number}`;
	projectDetails.hidden = true;
	projectDetails.setAttribute('aria-label', `Projectdetails voor ${project.title}`);
	toggle.setAttribute('aria-controls', projectDetails.id);
	const task = makeElement('p');
	task.append(makeElement('strong', '', 'De opdracht'), document.createTextNode(project.task));
	const role = makeElement('p');
	role.append(makeElement('strong', '', 'Mijn rol'), document.createTextNode(` ${project.role}`));
	const link = makeElement('a', 'case-link');
	link.href = project.link;
	link.append(document.createTextNode(project.linkText), makeElement('span', '', ' ↗'));
	link.lastElementChild.setAttribute('aria-hidden', 'true');
	projectDetails.append(task, role, link);

	toggle.addEventListener('click', () => {
		const expanded = toggle.getAttribute('aria-expanded') === 'true';
		toggle.setAttribute('aria-expanded', String(!expanded));
		toggle.setAttribute('aria-label', `${expanded ? 'Toon' : 'Verberg'} projectdetails voor ${project.title}`);
		projectDetails.hidden = expanded;
	});

	summary.append(info, figure, toggle);
	item.append(summary, projectDetails);
	return item;
}

const projectList = document.querySelector('#project-list');
const projectOrder = document.querySelector('#project-order');
const projectStatus = document.querySelector('#project-status');

if (projectList && projectOrder) {
	function renderProjects(announceChange = false) {
		projectList.replaceChildren();
		const orderedProjects = [...projects];
		if (projectOrder.value === 'descending') orderedProjects.reverse();
		orderedProjects.forEach((project) => projectList.append(createProjectCard(project)));
		if (announceChange && projectStatus) {
			const direction = projectOrder.value === 'descending' ? 'nieuw naar oud' : 'oud naar nieuw';
			projectStatus.textContent = `Projecten gesorteerd van ${direction}. ${orderedProjects.length} projecten.`;
		}
	}

	projectOrder.addEventListener('change', () => renderProjects(true));
	renderProjects();
}

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
		const mailLink = makeElement('a', '', 'Open je e-mailprogramma om het bericht te versturen');
		mailLink.href = `mailto:raileyboer04@gmail.com?subject=${subject}&body=${body}`;
		formStatus.replaceChildren(makeElement('span', '', 'Je gegevens zijn gecontroleerd. '), mailLink);
	});
}

const weatherStatus = document.querySelector('#motor-weather-status');

if (weatherStatus) {
	const weatherResult = document.querySelector('#motor-weather-result');
	const weatherForm = document.querySelector('#motor-weather-form');
	const weatherLocation = document.querySelector('#motor-weather-location');
	const weatherSearch = document.querySelector('#motor-weather-search');
	const refreshWeather = document.querySelector('#motor-weather-refresh');
	let weatherLoaded = false;
	const weatherCodes = new Map([
		[0, 'helder'], [1, 'meestal helder'], [2, 'gedeeltelijk bewolkt'], [3, 'bewolkt'],
		[45, 'mistig'], [48, 'mistig'], [51, 'lichte motregen'], [53, 'motregen'], [55, 'dichte motregen'],
		[61, 'lichte regen'], [63, 'regen'], [65, 'hevige regen'], [71, 'lichte sneeuw'], [73, 'sneeuw'],
		[75, 'hevige sneeuw'], [80, 'regenbuien'], [81, 'regenbuien'], [82, 'hevige regenbuien'],
		[95, 'onweer'], [96, 'onweer met hagel'], [99, 'onweer met hagel']
	]);

	async function loadMotorWeather() {
		weatherLoaded = true;
		const query = weatherLocation.value.trim();
		weatherStatus.textContent = 'Locatie zoeken…';
		weatherResult.replaceChildren();
		weatherSearch.disabled = true;
		refreshWeather.disabled = true;

		try {
			const geocodingUrl = `https://geocoding-api.open-meteo.com/v1/search?name=${encodeURIComponent(query)}&count=1&language=nl&format=json`;
			const geocodingResponse = await fetch(geocodingUrl);
			if (!geocodingResponse.ok) throw new Error('De locatie kon niet worden opgezocht.');
			const geocodingData = await geocodingResponse.json();
			const location = geocodingData.results?.[0];

			if (!location) {
				weatherStatus.textContent = 'Geen locatie gevonden';
				weatherResult.append(makeElement('p', '', 'Controleer de plaatsnaam en probeer het opnieuw.'));
				return;
			}

			weatherStatus.textContent = `Weer ophalen voor ${location.name}…`;
			const forecastParams = new URLSearchParams({
				latitude: location.latitude,
				longitude: location.longitude,
				current: 'temperature_2m,precipitation,wind_speed_10m,weather_code',
				hourly: 'precipitation_probability',
				forecast_days: '1',
				timezone: 'auto'
			});
			const response = await fetch(`https://api.open-meteo.com/v1/forecast?${forecastParams}`);
			if (!response.ok) throw new Error('Het weerbericht is nu niet beschikbaar.');
			const data = await response.json();
			const current = data.current;
			const hour = data.hourly.time.findIndex((time) => time >= current.time.slice(0, 13));
			const rainChances = data.hourly.precipitation_probability.slice(Math.max(hour, 0), Math.max(hour, 0) + 3);
			const rainChance = Math.max(0, ...rainChances);
			let advice = 'Prima motorweer';
			let adviceText = 'De omstandigheden zien er goed uit voor een rit.';

			if (current.precipitation >= 0.3 || rainChance >= 60 || current.wind_speed_10m >= 50) {
				advice = 'Rit liever uitstellen';
				adviceText = 'Regen of harde wind maakt het minder prettig en veilig op de motor.';
			} else if (rainChance >= 30 || current.wind_speed_10m >= 30 || current.temperature_2m < 8) {
				advice = 'Rijden kan, wees voorbereid';
				adviceText = 'Houd rekening met kans op regen, wind of lage temperatuur.';
			}

			weatherStatus.textContent = 'Weerbericht bijgewerkt.';
			const facts = makeElement('ul', 'weather-facts');
			[
				`${location.name}${location.country ? `, ${location.country}` : ''}: ${Math.round(current.temperature_2m)} °C, ${weatherCodes.get(current.weather_code) || 'wisselvallig'}`,
				`Regenkans komende uren: ${rainChance}%`,
				`Wind: ${Math.round(current.wind_speed_10m)} km/u`
			].forEach((fact) => facts.append(makeElement('li', '', fact)));
			weatherResult.append(makeElement('p', '', adviceText), facts);
		} catch {
			weatherStatus.textContent = 'Weerbericht niet beschikbaar';
			weatherResult.append(makeElement('p', '', 'Het actuele weer kon niet worden opgehaald. Probeer het later opnieuw.'));
		} finally {
			weatherSearch.disabled = false;
			refreshWeather.disabled = false;
		}
	}

	weatherForm.addEventListener('submit', (event) => {
		event.preventDefault();
		loadMotorWeather();
	});
	refreshWeather.addEventListener('click', loadMotorWeather);
	const weatherObserver = new IntersectionObserver((entries, observer) => {
		if (entries.some((entry) => entry.isIntersecting) && !weatherLoaded) {
			observer.disconnect();
			loadMotorWeather();
		}
	}, { rootMargin: '300px' });
	weatherObserver.observe(weatherStatus);
}

const ageElement = document.querySelector('[data-age]');

if (ageElement) {
	const birthDate = new Date(`${ageElement.dataset.birthDate}T00:00:00`);
	const today = new Date();
	let age = today.getFullYear() - birthDate.getFullYear();
	if (today < new Date(today.getFullYear(), birthDate.getMonth(), birthDate.getDate())) age -= 1;
	ageElement.textContent = age;
}
