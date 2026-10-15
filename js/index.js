function makeElement(tag, className, text) {
	const element = document.createElement(tag);
	if (className) element.className = className;
	if (text) element.textContent = text;
	return element;
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
			let adviceText = 'De omstandigheden zien er goed uit voor een rit.';

			if (current.precipitation >= 0.3 || rainChance >= 60 || current.wind_speed_10m >= 50) {
				adviceText = 'Regen of harde wind maakt het minder prettig en veilig op de motor.';
			} else if (rainChance >= 30 || current.wind_speed_10m >= 30 || current.temperature_2m < 8) {
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
