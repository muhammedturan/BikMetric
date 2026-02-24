<template>
  <div class="wizard-page">
    <h1 class="page-title">Chart Wizard</h1>

    <!-- Mode Toggle -->
    <div class="mode-toggle">
      <button class="toggle-btn" :class="{ active: mode === 'manual' }" @click="mode = 'manual'">
        Manuel
      </button>
      <button class="toggle-btn" :class="{ active: mode === 'ai' }" @click="mode = 'ai'">
        AI ile Grafik
      </button>
    </div>

    <!-- ==================== MANUAL MODE ==================== -->
    <template v-if="mode === 'manual'">
      <!-- Steps indicator -->
      <div class="steps">
        <div v-for="(s, i) in stepLabels" :key="i" class="step" :class="{ active: step === i, done: step > i }">
          <div class="step-num">{{ step > i ? '✓' : i + 1 }}</div>
          <span>{{ s }}</span>
        </div>
      </div>

      <!-- Step 0: Data source -->
      <div class="card" v-if="step === 0">
        <div class="card-header"><h2>Veri Kaynagi</h2></div>
        <div class="card-body">
          <p class="hint" v-if="!ruleStore.rules.length">
            Henuz kayitli kural yok. Once Kural Olusturucu'dan bir kural kaydedin.
          </p>
          <div v-for="rule in ruleStore.rules" :key="rule.id" class="rule-option"
            :class="{ selected: selectedRuleId === rule.id }" @click="selectedRuleId = rule.id">
            <strong>{{ rule.name }}</strong>
            <span class="rule-meta">{{ rule.tableName }} - {{ rule.filters.length }} filtre</span>
          </div>
          <div class="actions" v-if="ruleStore.rules.length">
            <button class="btn btn-primary" :disabled="!selectedRuleId" @click="loadRuleData">Devam</button>
          </div>
        </div>
      </div>

      <!-- Step 1: Chart type -->
      <div class="card" v-if="step === 1">
        <div class="card-header"><h2>Grafik Tipi</h2></div>
        <div class="card-body">
          <div class="chart-types">
            <div class="chart-type-card" :class="{ selected: chartType === 'bar' }" @click="chartType = 'bar'">
              <div class="chart-type-icon">📊</div>
              <div>Bar Chart</div>
            </div>
            <div class="chart-type-card" :class="{ selected: chartType === 'pie' }" @click="chartType = 'pie'">
              <div class="chart-type-icon">🥧</div>
              <div>Pie Chart</div>
            </div>
          </div>
          <div class="actions">
            <button class="btn" @click="step = 0">Geri</button>
            <button class="btn btn-primary" :disabled="!chartType" @click="step = 2">Devam</button>
          </div>
        </div>
      </div>

      <!-- Step 2: Configure -->
      <div class="card" v-if="step === 2">
        <div class="card-header"><h2>Yapilandirma</h2></div>
        <div class="card-body">
          <div class="config-grid">
            <div class="field">
              <label>{{ chartType === 'bar' ? 'X Ekseni (Kategori)' : 'Label (Kategori)' }}</label>
              <select v-model="labelColumn" class="select">
                <option value="">Secin...</option>
                <option v-for="col in stringColumns" :key="col" :value="col">{{ col }}</option>
              </select>
            </div>
            <div class="field">
              <label>{{ chartType === 'bar' ? 'Y Ekseni (Deger)' : 'Deger' }}</label>
              <select v-model="valueColumn" class="select">
                <option value="">Secin...</option>
                <option v-for="col in numberColumns" :key="col" :value="col">{{ col }}</option>
              </select>
            </div>
            <div class="field">
              <label>Toplama Fonksiyonu</label>
              <select v-model="aggregation" class="select">
                <option value="sum">Toplam</option>
                <option value="avg">Ortalama</option>
                <option value="count">Sayi</option>
              </select>
            </div>
          </div>
          <div class="actions">
            <button class="btn" @click="step = 1">Geri</button>
            <button class="btn btn-primary" :disabled="!labelColumn || !valueColumn" @click="generateChart">Onizle</button>
          </div>
        </div>
      </div>

      <!-- Step 3: Preview -->
      <div class="card" v-if="step === 3">
        <div class="card-header">
          <h2>Onizleme</h2>
          <button class="btn btn-sm" @click="step = 2">Duzenle</button>
        </div>
        <div class="card-body chart-container">
          <Bar v-if="chartType === 'bar'" :data="chartData" :options="chartOptions" />
          <Pie v-if="chartType === 'pie'" :data="chartData" :options="chartOptions" />
        </div>
      </div>
    </template>

    <!-- ==================== AI MODE ==================== -->
    <template v-if="mode === 'ai'">
      <!-- AI Input -->
      <div class="card">
        <div class="card-header">
          <h2>AI ile Grafik Olustur</h2>
        </div>
        <div class="card-body">
          <div class="ai-input-wrapper">
            <textarea
              v-model="aiQuestion"
              class="ai-input"
              placeholder="Orn: Kategorilere gore toplam goruntuleme bar chart olarak goster"
              rows="3"
              @keydown.ctrl.enter="generateChartFromAi"
            />
            <button class="btn btn-primary btn-lg" @click="generateChartFromAi" :disabled="aiLoading || !aiQuestion.trim()">
              {{ aiLoading ? 'AI dusunuyor...' : 'Grafik Olustur' }}
            </button>
          </div>
          <div class="examples">
            <span class="examples-label">Ornekler:</span>
            <button v-for="ex in aiChartExamples" :key="ex" class="example-chip" @click="aiQuestion = ex">
              {{ ex }}
            </button>
          </div>
        </div>
      </div>

      <!-- AI Error -->
      <div class="card" v-if="aiError">
        <div class="card-body">
          <p class="error-text">{{ aiError }}</p>
        </div>
      </div>

      <!-- AI Generated SQL -->
      <div class="card" v-if="aiResult">
        <div class="card-header">
          <h2>Uretilen SQL</h2>
        </div>
        <div class="card-body">
          <pre class="sql-block">{{ aiResult.generatedSql }}</pre>
          <p class="explanation" v-if="aiResult.explanation">{{ aiResult.explanation }}</p>
        </div>
      </div>

      <!-- AI Chart -->
      <div class="card" v-if="aiResult && aiCanChart">
        <div class="card-header">
          <h2>Grafik</h2>
          <div class="chart-toggle">
            <button class="toggle-btn" :class="{ active: aiChartType === 'bar' }" @click="aiChartType = 'bar'">Bar</button>
            <button class="toggle-btn" :class="{ active: aiChartType === 'pie' }" @click="aiChartType = 'pie'">Pie</button>
          </div>
        </div>
        <div class="card-body chart-container">
          <Bar v-if="aiChartType === 'bar'" :data="aiChartData" :options="aiChartOptions" />
          <Pie v-if="aiChartType === 'pie'" :data="aiChartData" :options="aiChartOptions" />
        </div>
      </div>

      <!-- AI Data Table -->
      <div class="card" v-if="aiResult">
        <div class="card-header">
          <h2>Veriler ({{ aiResult.totalCount }} kayit)</h2>
        </div>
        <div class="card-body table-wrapper">
          <table class="data-table">
            <thead>
              <tr>
                <th v-for="col in aiResult.columns" :key="col">{{ col }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(row, i) in aiResult.rows" :key="i">
                <td v-for="col in aiResult.columns" :key="col">{{ row[col] }}</td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { Bar, Pie } from 'vue-chartjs'
import {
  Chart as ChartJS, Title, Tooltip, Legend, ArcElement,
  CategoryScale, LinearScale, BarElement,
} from 'chart.js'
import api from '../../services/api'
import { useRuleStore } from '../../stores/ruleStore'

ChartJS.register(Title, Tooltip, Legend, ArcElement, CategoryScale, LinearScale, BarElement)

const ruleStore = useRuleStore()

const COLORS = [
  '#6366f1', '#0ea5e9', '#10b981', '#f59e0b', '#ef4444',
  '#8b5cf6', '#ec4899', '#14b8a6', '#f97316', '#06b6d4',
  '#84cc16', '#e11d48', '#7c3aed', '#0891b2', '#65a30d',
]

// ==================== MODE ====================
const mode = ref<'manual' | 'ai'>('manual')

// ==================== MANUAL MODE STATE ====================
const step = ref(0)
const stepLabels = ['Veri Kaynagi', 'Grafik Tipi', 'Yapilandirma', 'Onizleme']

const selectedRuleId = ref('')
const chartType = ref('')
const labelColumn = ref('')
const valueColumn = ref('')
const aggregation = ref('sum')
const queryResult = ref<any>(null)
const chartData = ref<any>({ labels: [], datasets: [] })
const chartOptions = ref<any>({})

const stringColumns = computed(() =>
  queryResult.value?.columns?.filter((c: string) => {
    const row = queryResult.value.rows[0]
    return row && typeof row[c] === 'string'
  }) || []
)

const numberColumns = computed(() =>
  queryResult.value?.columns?.filter((c: string) => {
    const row = queryResult.value.rows[0]
    return row && typeof row[c] === 'number'
  }) || []
)

async function loadRuleData() {
  const rule = ruleStore.rules.find((r) => r.id === selectedRuleId.value)
  if (!rule) return

  try {
    const { data } = await api.post('/query/execute', {
      tableName: rule.tableName,
      filters: rule.filters,
    })
    queryResult.value = data
    step.value = 1
  } catch (e: any) {
    alert(e.response?.data?.message || 'Veri yuklenemedi')
  }
}

function generateChart() {
  const rows = queryResult.value.rows
  const grouped: Record<string, number[]> = {}

  for (const row of rows) {
    const label = String(row[labelColumn.value])
    if (!grouped[label]) grouped[label] = []
    grouped[label].push(Number(row[valueColumn.value]) || 0)
  }

  const labels = Object.keys(grouped)
  const values = labels.map((label) => {
    const arr = grouped[label] ?? []
    if (aggregation.value === 'sum') return arr.reduce((a, b) => a + b, 0)
    if (aggregation.value === 'avg') return arr.length > 0 ? arr.reduce((a, b) => a + b, 0) / arr.length : 0
    return arr.length // count
  })

  chartData.value = {
    labels,
    datasets: [{
      label: `${valueColumn.value} (${aggregation.value})`,
      data: values,
      backgroundColor: chartType.value === 'pie' ? COLORS.slice(0, labels.length) : COLORS[0],
      borderRadius: chartType.value === 'bar' ? 6 : 0,
    }],
  }

  chartOptions.value = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: { position: chartType.value === 'pie' ? 'right' as const : 'top' as const },
    },
  }

  step.value = 3
}

// ==================== AI MODE STATE ====================
const aiQuestion = ref('')
const aiLoading = ref(false)
const aiError = ref('')
const aiResult = ref<any>(null)
const aiChartType = ref<'bar' | 'pie'>('bar')

const aiChartExamples = [
  'Kategorilere gore toplam goruntuleme',
  'Haber sitelerine gore ortalama okuma suresi',
  'En cok paylasilan 10 haberin basliklari ve paylasim sayilari',
  'Yazarlara gore toplam tiklanma sayisi',
  'Ulkelere gore haber sitesi sayisi',
]

const aiCanChart = computed(() => {
  if (!aiResult.value || aiResult.value.rows.length === 0) return false
  const row = aiResult.value.rows[0]
  const hasString = aiResult.value.columns.some((c: string) => typeof row[c] === 'string')
  const hasNumber = aiResult.value.columns.some((c: string) => typeof row[c] === 'number')
  return hasString && hasNumber
})

const aiChartData = computed(() => {
  if (!aiResult.value) return { labels: [], datasets: [] }

  const rows = aiResult.value.rows
  const cols = aiResult.value.columns as string[]
  const firstRow = rows[0]

  const labelCol = cols.find((c) => typeof firstRow[c] === 'string') ?? cols[0]
  const valueCols = cols.filter((c) => typeof firstRow[c] === 'number')

  const labels = rows.map((r: any) => String(r[labelCol]))

  if (aiChartType.value === 'pie') {
    const valCol = valueCols[0] ?? cols[0]
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

const aiChartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { position: (aiChartType.value === 'pie' ? 'right' : 'top') as 'right' | 'top' },
  },
}))

async function generateChartFromAi() {
  if (!aiQuestion.value.trim()) return
  aiLoading.value = true
  aiError.value = ''
  aiResult.value = null

  try {
    const { data } = await api.post('/ai/query', { question: aiQuestion.value })
    aiResult.value = data
    const q = aiQuestion.value.toLowerCase()
    if (q.includes('pie') || q.includes('pasta') || q.includes('daire')) {
      aiChartType.value = 'pie'
    } else {
      aiChartType.value = 'bar'
    }
  } catch (e: any) {
    aiError.value = e.response?.data?.message || 'AI grafik uretimi basarisiz oldu'
  } finally {
    aiLoading.value = false
  }
}

onMounted(() => ruleStore.loadRules())
</script>

<style scoped>
.page-title { font-size: 24px; font-weight: 700; margin-bottom: 24px; }

.mode-toggle {
  display: flex;
  gap: 4px;
  background: var(--gray-100);
  border-radius: var(--radius);
  padding: 4px;
  width: fit-content;
  margin-bottom: 20px;
}
.toggle-btn {
  padding: 8px 20px;
  border-radius: var(--radius);
  font-size: 13px;
  font-weight: 500;
  background: transparent;
  color: var(--gray-500);
  border: none;
  cursor: pointer;
  transition: all 0.15s;
}
.toggle-btn.active {
  background: white;
  color: var(--gray-800);
  box-shadow: var(--shadow);
}

.chart-toggle {
  display: flex; gap: 4px; background: var(--gray-100);
  border-radius: var(--radius); padding: 3px;
}

.ai-input-wrapper { display: flex; gap: 12px; align-items: flex-end; margin-bottom: 16px; }
.ai-input {
  flex: 1; padding: 12px 16px; border: 2px solid var(--gray-200);
  border-radius: var(--radius-lg); font-size: 15px; resize: none;
  font-family: var(--font); line-height: 1.5;
}
.ai-input:focus { border-color: var(--primary); outline: none; box-shadow: 0 0 0 3px var(--primary-light); }
.btn-lg { padding: 12px 24px; font-size: 14px; border-radius: var(--radius-lg); white-space: nowrap; }

.examples { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; }
.examples-label { font-size: 12px; color: var(--gray-400); font-weight: 500; }
.example-chip {
  padding: 4px 12px; border-radius: 20px; background: var(--gray-100);
  font-size: 12px; color: var(--gray-600); border: none; cursor: pointer;
  transition: all 0.15s;
}
.example-chip:hover { background: var(--primary-light); color: var(--primary); }

.explanation {
  margin-top: 12px; color: var(--gray-600); font-size: 14px;
  padding: 12px; background: var(--gray-50); border-radius: var(--radius);
  line-height: 1.5;
}

.sql-block {
  background: var(--dark); color: #e2e8f0; padding: 16px;
  border-radius: var(--radius); font-size: 13px; line-height: 1.6;
  overflow-x: auto; font-family: 'Fira Code', 'Consolas', monospace;
  margin-bottom: 12px;
}

.error-text { color: var(--danger); font-size: 14px; }

.steps { display: flex; gap: 8px; margin-bottom: 24px; }
.step {
  display: flex; align-items: center; gap: 8px; padding: 8px 16px;
  border-radius: 20px; background: var(--gray-100); font-size: 13px;
  color: var(--gray-500); font-weight: 500;
}
.step.active { background: var(--primary); color: white; }
.step.done { background: #dcfce7; color: var(--success); }
.step-num {
  width: 24px; height: 24px; border-radius: 50%;
  display: flex; align-items: center; justify-content: center;
  font-size: 12px; font-weight: 700;
  background: rgba(255,255,255,0.2);
}

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

.hint { color: var(--gray-400); font-size: 14px; }

.rule-option {
  padding: 12px 16px; border: 2px solid var(--gray-100); border-radius: var(--radius);
  margin-bottom: 8px; cursor: pointer; transition: all 0.15s;
}
.rule-option:hover { border-color: var(--gray-300); }
.rule-option.selected { border-color: var(--primary); background: var(--primary-light); }
.rule-meta { font-size: 12px; color: var(--gray-400); margin-left: 8px; }

.chart-types { display: flex; gap: 16px; margin-bottom: 20px; }
.chart-type-card {
  flex: 1; padding: 24px; border: 2px solid var(--gray-100);
  border-radius: var(--radius-lg); text-align: center; cursor: pointer;
  transition: all 0.15s; font-weight: 600;
}
.chart-type-card:hover { border-color: var(--gray-300); }
.chart-type-card.selected { border-color: var(--primary); background: var(--primary-light); }
.chart-type-icon { font-size: 48px; margin-bottom: 8px; }

.config-grid { display: grid; grid-template-columns: repeat(3, 1fr); gap: 16px; margin-bottom: 20px; }
.field { display: flex; flex-direction: column; gap: 6px; }
.field label { font-size: 13px; font-weight: 600; color: var(--gray-700); }
.select {
  padding: 8px 12px; border: 1px solid var(--gray-200); border-radius: var(--radius);
  font-size: 14px; background: white;
}

.chart-container { height: 400px; }

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

.actions { margin-top: 16px; display: flex; gap: 8px; justify-content: flex-end; }
.btn {
  padding: 8px 16px; border-radius: var(--radius); font-size: 13px;
  font-weight: 500; background: var(--gray-100); color: var(--gray-700);
  border: none; cursor: pointer; transition: all 0.15s;
}
.btn:hover { background: var(--gray-200); }
.btn-primary { background: var(--primary); color: white; }
.btn-primary:hover:not(:disabled) { background: var(--primary-hover); }
.btn-primary:disabled { opacity: 0.6; cursor: not-allowed; }
.btn-sm { padding: 6px 12px; font-size: 12px; }
</style>
