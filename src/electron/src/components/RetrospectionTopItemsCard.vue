<script setup lang="ts">
import type { PropType } from 'vue';
import { Activity, Color, type ActivitySessions } from '../utils/retrospection/types';
import { getTailwindClassFromActivity } from '../utils/retrospection/utils';

defineProps({
  title: {
    type: String,
    required: true
  },
  items: {
    type: Array as PropType<ActivitySessions[]>,
    required: true
  }
});

function renderCompactTime(ms: number): string {
  const minutes = Math.round(ms / 60_000);
  if (minutes < 1) {
    return '< 1 min';
  }
  if (minutes < 60) {
    return `${minutes} min`;
  }

  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;
  return remainingMinutes ? `${hours} hr ${remainingMinutes} min` : `${hours} hr`;
}

function getTopItemWidth(item: ActivitySessions, items: ActivitySessions[]): string {
  const maxDurationMs = Math.max(...items.map((topItem) => topItem.totalDurationMs), 1);
  return `${Math.max((item.totalDurationMs / maxDurationMs) * 100, 8)}%`;
}

function getTopItemColor(item: ActivitySessions): string {
  const activity = item.activity || Activity.Other;
  const colorKey = getTailwindClassFromActivity(activity) as keyof typeof Color;
  return Color[colorKey] || Color['neutral-400'];
}

function getTopItemInitial(item: ActivitySessions): string {
  return item.type.trim().charAt(0).toUpperCase() || '?';
}
</script>

<template>
  <section
    class="top-item-card rounded border border-gray-200 bg-gray-100 px-4 py-3 text-gray-800 dark:border-transparent dark:bg-neutral-800 dark:text-slate-200"
  >
    <h2 class="primary-blue font-bold leading-4">{{ title }}</h2>
    <ol class="top-item-list">
      <li
        v-for="item in items"
        :key="item.type"
        class="top-item-row"
        :title="item.tooltipTitle || item.type"
      >
        <div class="top-item-content">
          <span class="top-item-label-group">
            <img v-if="item.iconDataUrl" :src="item.iconDataUrl" alt="" class="top-item-icon" />
            <span
              v-else
              class="top-item-icon top-item-icon-fallback"
              :style="{ backgroundColor: getTopItemColor(item) }"
              aria-hidden="true"
            >
              {{ getTopItemInitial(item) }}
            </span>
            <span class="top-item-label">{{ item.type }}</span>
          </span>
          <span class="top-item-time">{{ renderCompactTime(item.totalDurationMs) }}</span>
        </div>
        <div class="top-item-track">
          <div
            class="top-item-bar"
            :style="{
              width: getTopItemWidth(item, items),
              backgroundColor: getTopItemColor(item)
            }"
          ></div>
        </div>
      </li>
    </ol>
  </section>
</template>

<style lang="less" scoped>
@import '../styles/index';

h2.primary-blue {
  color: @primary-color;
}

.top-item-card {
  height: 100%;
  min-height: 148px;
}

.top-item-list {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  margin: 0.55rem 0 0;
  padding: 0;
  list-style: none;
}

.top-item-row {
  min-height: 0;
  padding: 0.1rem 0;
}

.top-item-content {
  display: grid;
  grid-template-columns: minmax(0, 1fr) max-content;
  align-items: center;
  gap: 0.65rem;
  margin-bottom: 0.2rem;
  line-height: 1.25rem;
}

.top-item-label-group {
  display: flex;
  align-items: center;
  min-width: 0;
  gap: 0.45rem;
}

.top-item-icon {
  width: 18px;
  height: 18px;
  border-radius: 4px;
  flex: 0 0 auto;
  object-fit: contain;
}

.top-item-icon-fallback {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  color: white;
  font-size: 0.65rem;
  font-weight: 700;
}

.top-item-label {
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  color: #1f2937;
}

.top-item-time {
  color: #374151;
  white-space: nowrap;
}

.top-item-track {
  height: 0.35rem;
  overflow: hidden;
  border-radius: 999px;
  background: #e5e7eb;
}

.top-item-bar {
  height: 100%;
  border-radius: inherit;
}

@media (prefers-color-scheme: dark) {
  .top-item-track {
    background: #111111;
  }

  .top-item-label {
    color: #e2e8f0;
  }

  .top-item-time {
    color: #cbd5e1;
  }
}
</style>
