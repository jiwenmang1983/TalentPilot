<template>
  <div class="login-page">
    <a-card class="login-card" :bordered="false">
      <div class="login-brand">
        <h1>TalentPilot</h1>
        <p>AI 智能招聘平台</p>
      </div>

      <a-form
        :model="formState"
        :rules="rules"
        @finish="handleLogin"
        layout="vertical"
      >
        <a-form-item label="用户名" name="username">
          <a-input
            v-model:value="formState.username"
            placeholder="请输入用户名"
            size="large"
          >
            <template #prefix>
              <UserOutlined />
            </template>
          </a-input>
        </a-form-item>

        <a-form-item label="密码" name="password">
          <a-input
            v-model:value="formState.password"
            :type="showPassword ? 'text' : 'password'"
            placeholder="请输入密码"
            size="large"
          >
            <template #prefix>
              <LockOutlined />
            </template>
            <template #suffix>
              <EyeOutlined
                v-if="showPassword"
                @click="showPassword = !showPassword"
                style="cursor: pointer; color: #8c8c8c"
              />
              <EyeInvisibleOutlined
                v-else
                @click="showPassword = !showPassword"
                style="cursor: pointer; color: #8c8c8c"
              />
            </template>
          </a-input>
        </a-form-item>

        <div class="forgot-password">
          <a href="#">忘记密码？</a>
        </div>

        <a-button
          type="primary"
          html-type="submit"
          size="large"
          block
          :loading="loading"
        >
          登 录
        </a-button>
      </a-form>

      <div class="login-copyright">© 2026 TalentPilot 版权所有</div>
    </a-card>
  </div>
</template>

<script setup>
import { reactive, ref } from 'vue'
import { useRouter } from 'vue-router'
import { UserOutlined, LockOutlined, EyeOutlined, EyeInvisibleOutlined } from '@ant-design/icons-vue'
import { message } from 'ant-design-vue'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()
const loading = ref(false)
const showPassword = ref(false)

const formState = reactive({
  username: '',
  password: ''
})

const rules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

async function handleLogin() {
  loading.value = true
  try {
    await authStore.login(formState.username, formState.password)
    message.success('登录成功')
    router.push('/')
  } catch (error) {
    const errMsg = error?.response?.data?.message || error?.message || '登录失败，请稍后重试'
    message.error(errMsg)
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #f0f2f5;
}

.login-card {
  width: 420px;
}

.login-brand {
  text-align: center;
  padding: 8px 0 24px;
}

.login-brand h1 {
  font-size: 24px;
  font-weight: 600;
  color: #262626;
  margin: 0 0 4px;
}

.login-brand p {
  font-size: 14px;
  color: #8c8c8c;
  margin: 0;
}

.forgot-password {
  text-align: right;
  margin-bottom: 16px;
}

.forgot-password a {
  font-size: 14px;
  color: #1677ff;
}

.login-copyright {
  text-align: center;
  font-size: 12px;
  color: #bfbfbf;
  margin-top: 24px;
}

@media (max-width: 480px) {
  .login-card {
    width: calc(100vw - 32px);
  }
}
</style>