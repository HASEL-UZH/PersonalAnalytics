<script setup lang="ts">

import studyConfig from '../../shared/study.config'

const considerWorkHours = studyConfig.trackers.experienceSamplingTracker.enabledWorkHours;

</script>


<template>
  <div class="settings-container">
    <nav class="sidenav">
      <ul>
        <li><router-link to="about">About</router-link></li>
        <li v-if="considerWorkHours"><router-link to="work-hours">Active Times</router-link></li>
        <!-- <li><router-link to="config">App Configuration</router-link></li> -->
      </ul>
    </nav>
    <div class="content">
      <router-view>
      </router-view>
    </div>
  </div>
</template>

<style scoped>
.settings-container {
  display: flex;
  height: 100vh;
}

.sidenav {
  width: 200px;
  padding: 20px;
  background-color: rgba(0, 0, 0, 0.1);
}

.sidenav ul {
  list-style-type: none;
  padding: 0;
}

.sidenav li {
  margin: 10px 0;
}

.sidenav a {
  text-decoration: none;
  cursor: pointer;
  border-radius: 12px;
  padding: 7px 20px;
  width: 100%;
  display: block;
}

.sidenav a:hover,
.sidenav a.router-link-active {
  background-color: #007acc84;
}

.content {
  flex: 1;
  padding: 20px;
}
</style>
