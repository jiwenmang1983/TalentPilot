<template>
  <div class="login-wrapper">
    <div class="login-card">
      <h1 class="login-brand">TalentPilot</h1>
      <p class="login-tagline">AI面试官</p>

      <a-form
        :model="formState"
        :rules="rules"
        @finish="handleLogin"
        layout="vertical"
        class="login-form"
      >
        <a-form-item name="username">
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

        <a-form-item name="password">
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
              <span class="password-toggle" @click="showPassword = !showPassword">
                <EyeOutlined v-if="showPassword" />
                <EyeInvisibleOutlined v-else />
              </span>
            </template>
          </a-input>
        </a-form-item>

        <div class="form-footer">
          <a href="#" class="forgot-link">忘记密码？</a>
        </div>

        <a-button
          type="primary"
          html-type="submit"
          size="large"
          block
          :loading="loading"
          class="login-btn"
        >
          登录
        </a-button>
      </a-form>

      <div class="login-footer">© 2026 TalentPilot 版权所有</div>
    </div>
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
.login-wrapper {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #e8f0fe 0%, #d4e4ff 40%, #e8f0fe 100%);
}

.login-card {
  width: 420px;
  padding: 40px;
  background: #ffffff;
  border-radius: 8px;
  box-shadow: 0 4px 24px rgba(102, 126, 234, 0.12);
}

.login-brand {
  font-size: 26px;
  font-weight: 700;
  color: #1a1a2e;
  text-align: center;
  margin-bottom: 4px;
}

.login-tagline {
  font-size: 14px;
  color: #8c8ca1;
  text-align: center;
  margin-bottom: 24px;
}

.login-form {
  /* empty */
}

.form-footer {
  text-align: right;
  margin-bottom: 16px;
}

.forgot-link {
  font-size: 12px;
  color: #1677ff;
  text-decoration: none;
}

.forgot-link:hover {
  text-decoration: underline;
}

.login-btn {
  height: 40px;
  border-radius: 4px;
  transition: box-shadow 0.3s;
}

.login-btn:hover {
  box-shadow: 0 4px 12px rgba(22, 119, 255, 0.35);
}

.password-toggle {
  cursor: pointer;
  color: #8c8ca1;
  display: flex;
  align-items: center;
}

.password-toggle:hover {
  color: #1677ff;
}

.login-footer {
  text-align: center;
  font-size: 12px;
  color: #b8b8d0;
  margin-top: 24px;
}

@media (max-width: 480px) {
  .login-card {
    width: 90vw;
    padding: 24px;
  }
}
</style>