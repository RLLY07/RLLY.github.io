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
