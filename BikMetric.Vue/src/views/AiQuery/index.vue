<template>
  <div class="ai-page">
    <h1 class="page-title">AI Sorgu</h1>

    <!-- Query Input -->
    <div class="card">
      <div class="card-header">
        <h2>Dogal Dil ile Sorgula</h2>
      </div>
      <div class="card-body">
        <div class="ai-input-wrapper">
          <textarea
            v-model="question"
            class="ai-input"
            placeholder="Orn: En cok okunan haber sitesi hangisi? / Kategorilere gore toplam goruntuleme / Hangi yazar en cok tiklanma almis?"
            rows="3"
            @keydown.ctrl.enter="askAi"
          />
          <button class="btn btn-primary btn-lg" @click="askAi" :disabled="loading || !question.trim()">
            {{ loading ? 'AI dusunuyor...' : 'Sorgula' }}
          </button>
        </div>

        <div class="examples">
          <span class="examples-label">Ornekler:</span>
          <button v-for="ex in examples" :key="ex" class="example-chip" @click="question = ex">
            {{ ex }}
          </button>
        </div>
      </div>
    </div>

    <!-- Error -->
    <div class="card error-card" v-if="error">
      <div class="card-body">
        <p class="error-text">{{ error }}</p>
      </div>
    </div>

    <!-- Result -->
    <div v-if="result" class="result-section">
      <!-- SQL -->
      <div class="card">
        <div class="card-header">
          <h2>Uretilen SQL</h2>
        </div>
        <div class="card-body">
          <pre class="sql-block">{{ result.generatedSql }}</pre>
          <p class="explanation" v-if="result.explanation">{{ result.explanation }}</p>
        </div>
      </div>

      <!-- Data Table -->
      <div class="card">
        <div class="card-header">
          <h2>Sonuclar ({{ result.totalCount }} kayit)</h2>
        </div>
        <div class="card-body table-wrapper">
          <table class="data-table">
            <thead>
              <tr>
                <th v-for="col in result.columns" :key="col">{{ col }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(row, i) in result.rows" :key="i">
                <td v-for="col in result.columns" :key="col">{{ formatValue(row[col]) }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>

      <!-- Quick Chart -->
      <div class="card" v-if="canChart">
        <div class="card-header">
          <h2>Grafik</h2>
          <div class="chart-toggle">
            <button class="btn btn-sm" :class="{ active: vizType === 'bar' }" @click="vizType = 'bar'">Bar</button>
            <button class="btn btn-sm" :class="{ active: vizType === 'pie' }" @click="vizType = 'pie'">Pie</button>
          </div>
        </div>
        <div class="card-body chart-container">
          <Bar v-if="vizType === 'bar'" :data="autoChartData" :options="chartOptions" />
          <Pie v-if="vizType === 'pie'" :data="autoChartData" :options="chartOptions" />
        </div>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { Bar, Pie } from 'vue-chartjs'
import {
  Chart as ChartJS, Title, Tooltip, Legend, ArcElement,
  CategoryScale, LinearScale, BarElement,
} from 'chart.js'
import api from '../../services/api'

ChartJS.register(Title, Tooltip, Legend, ArcElement, CategoryScale, LinearScale, BarElement)

const question = ref('')
const result = ref<any>(null)
const loading = ref(false)
const error = ref('')
const vizType = ref('bar')

const examples = [
  'En cok okunan haber sitesi hangisi?',
  'Kategorilere gore toplam goruntuleme sayisi',
  'Son 30 gunde en cok tiklanan 10 haber',
  'Hangi yazar en cok paylasim almis?',
  'Haber sitelerine gore ortalama okuma suresi',
]

const COLORS = [
  '#6366f1', '#0ea5e9', '#10b981', '#f59e0b', '#ef4444',
  '#8b5cf6', '#ec4899', '#14b8a6', '#f97316', '#06b6d4',
]

const canChart = computed(() => {
  if (!result.value || result.value.rows.length === 0) return false
  const row = result.value.rows[0]
  const hasString = result.value.columns.some((c: string) => typeof row[c] === 'string')
  const hasNumber = result.value.columns.some((c: string) => typeof row[c] === 'number')
  return hasString && hasNumber
})

const autoChartData = computed(() => {
  if (!result.value) return { labels: [], datasets: [] }

  const rows = result.value.rows
  const cols = result.value.columns as string[]
  const firstRow = rows[0]

  const labelCol = cols.find((c) => typeof firstRow[c] === 'string') || cols[0]
  const valueCols = cols.filter((c) => typeof firstRow[c] === 'number')

  const labels = rows.map((r: any) => String(r[labelCol]))

  if (vizType.value === 'pie') {
    const valCol = valueCols[0]
    return {
      labels,
      datasets: [{
        data: rows.map((r: any) => Number(r[valCol]) || 0),
        backgroundColor: COLORS.slice(0, labels.length),
      }],
    }
  }

  return {
    labels,
    datasets: valueCols.map((valCol, idx) => ({
      label: valCol,
      data: rows.map((r: any) => Number(r[valCol]) || 0),
      backgroundColor: COLORS[idx % COLORS.length],
      borderRadius: 6,
    })),
  }
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { position: vizType.value === 'pie' ? 'right' as const : 'top' as const },
  },
}))

function formatValue(val: any) {
  if (typeof val === 'number') {
    return val % 1 === 0 ? val.toLocaleString('tr-TR') : val.toLocaleString('tr-TR', { minimumFractionDigits: 2 })
  }
  return val
}

async function askAi() {
  if (!question.value.trim()) return
  loading.value = true
  error.value = ''
  result.value = null

  try {
    const { data } = await api.post('/ai/query', { question: question.value })
    result.value = data
  } catch (e: any) {
    error.value = e.response?.data?.message || 'AI sorgusu basarisiz oldu'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.page-title { font-size: 24px; font-weight: 700; margin-bottom: 24px; }

.card {
  background: white; border-radius: var(--radius-lg);
  box-shadow: var(--shadow); margin-bottom: 16px;
}
.card-header {
  padding: 16px 20px; border-bottom: 1px solid var(--gray-100);
  display: flex; align-items: center; justify-content: space-between;
}
.card-header h2 { font-size: 16px; font-weight: 600; }
.card-body { padding: 20px; }

.ai-input-wrapper { display: flex; gap: 12px; align-items: flex-end; margin-bottom: 16px; }
.ai-input {
  flex: 1; padding: 12px 16px; border: 2px solid var(--gray-200);
  border-radius: var(--radius-lg); font-size: 15px; resize: none;
  font-family: var(--font); line-height: 1.5;
}
.ai-input:focus { border-color: var(--primary); box-shadow: 0 0 0 3px var(--primary-light); }

.btn {
  padding: 8px 16px; border-radius: var(--radius); font-size: 13px;
  font-weight: 500; background: var(--gray-100); color: var(--gray-700);
  transition: all 0.15s;
}
.btn:hover { background: var(--gray-200); }
.btn-primary { background: var(--primary); color: white; }
.btn-primary:hover:not(:disabled) { background: var(--primary-hover); }
.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
.btn-lg { padding: 12px 24px; font-size: 14px; border-radius: var(--radius-lg); }
.btn-sm { padding: 6px 12px; font-size: 12px; }
.btn-sm.active { background: var(--primary); color: white; }

.examples { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
.examples-label { font-size: 12px; color: var(--gray-400); font-weight: 500; }
.example-chip {
  padding: 4px 12px; border-radius: 20px; background: var(--gray-100);
  font-size: 12px; color: var(--gray-600); transition: all 0.15s;
}
.example-chip:hover { background: var(--primary-light); color: var(--primary); }

.error-card { border-left: 4px solid var(--danger); }
.error-text { color: var(--danger); font-size: 14px; }

.sql-block {
  background: var(--gray-900); color: #e2e8f0; padding: 16px;
  border-radius: var(--radius); font-size: 13px; overflow-x: auto;
  font-family: 'SF Mono', 'Fira Code', monospace; line-height: 1.6;
}
.explanation {
  margin-top: 12px; color: var(--gray-600); font-size: 14px;
  padding: 12px; background: var(--gray-50); border-radius: var(--radius);
  line-height: 1.5;
}

.table-wrapper { overflow-x: auto; }
.data-table { width: 100%; border-collapse: collapse; font-size: 13px; }
.data-table th {
  text-align: left; padding: 10px 12px; background: var(--gray-50);
  border-bottom: 2px solid var(--gray-200); font-weight: 600; white-space: nowrap;
}
.data-table td {
  padding: 8px 12px; border-bottom: 1px solid var(--gray-100); white-space: nowrap;
}
.data-table tr:hover td { background: var(--gray-50); }

.chart-toggle { display: flex; gap: 4px; }
.chart-container { height: 400px; }
</style>
