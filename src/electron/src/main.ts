import { createApp } from 'vue';
import router from './router';
import App from './App.vue';

import './styles/tailwind.css';
import './styles/index.less';
import './styles/tailwind-apply.css';

const app = createApp(App);
app.use(router);
app.mount('#app');
