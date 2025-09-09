######################################################################################################
cat <<'EOF'> ~/.zshrc
# Enable Powerlevel10k instant prompt. Should stay close to the top of ~/.zshrc.
# Initialization code that may require console input (password prompts, [y/n]
# confirmations, etc.) must go above this block; everything else may go below.
if [[ -r "${XDG_CACHE_HOME:-$HOME/.cache}/p10k-instant-prompt-${(%):-%n}.zsh" ]]; then
  source "${XDG_CACHE_HOME:-$HOME/.cache}/p10k-instant-prompt-${(%):-%n}.zsh"
fi
autoload -U +X bashcompinit && bashcompinit
autoload -U +X compinit && compinit
export TERM="xterm-256color"
HISTSIZE=10000
SAVEHIST=10000
HISTFILE=~/.zsh_history
setopt EXTENDED_HISTORY
setopt HIST_EXPIRE_DUPS_FIRST
setopt HIST_IGNORE_DUPS
setopt HIST_IGNORE_ALL_DUPS
setopt HIST_IGNORE_SPACE
setopt HIST_FIND_NO_DUPS
setopt HIST_SAVE_NO_DUPS
setopt HIST_BEEP
export PATH=$HOME/.local/bin:$PATH
EOF

######################################################################################################

git clone --depth=1 https://github.com/zsh-users/zsh-autosuggestions ~/.zsh/zsh-autosuggestions
echo 'source ~/.zsh/zsh-autosuggestions/zsh-autosuggestions.zsh' >> ~/.zshrc
git clone --depth=1 https://github.com/zsh-users/zsh-syntax-highlighting.git ~/.zsh/zsh-syntax-highlighting
echo 'source ~/.zsh/zsh-syntax-highlighting/zsh-syntax-highlighting.zsh' >> ~/.zshrc
git clone --depth=1 https://github.com/romkatv/powerlevel10k.git ~/powerlevel10k
echo 'source ~/powerlevel10k/powerlevel10k.zsh-theme' >> ~/.zshrc
echo '[[ ! -f ~/.p10k.zsh ]] || source ~/.p10k.zsh' >> ~/.zshrc
cp ~/powerlevel10k/config/p10k-pure.zsh ~/.p10k.zsh
sed -i '/vcs\s*# git status/a \ \   kubecontext               # kubernetes context' ~/.p10k.zsh
echo 'typeset -g POWERLEVEL9K_KUBECONTEXT_SHOW_DEFAULT_NAMESPACE=false' >> ~/.p10k.zsh

######################################################################################################

(
mkdir -p ~/.local/share/fonts
cd ~/.local/share/fonts
wget https://github.com/romkatv/powerlevel10k-media/raw/master/MesloLGS%20NF%20Regular.ttf
wget https://github.com/romkatv/powerlevel10k-media/raw/master/MesloLGS%20NF%20Bold.ttf
wget https://github.com/romkatv/powerlevel10k-media/raw/master/MesloLGS%20NF%20Italic.ttf
wget https://github.com/romkatv/powerlevel10k-media/raw/master/MesloLGS%20NF%20Bold%20Italic.ttf
)

######################################################################################################

mkdir -p ~/.local/bin

######################################################################################################

KUBECTL_VERSION=$(curl -L -s https://dl.k8s.io/release/stable-1.32.txt)
curl -fsSL https://dl.k8s.io/release/$KUBECTL_VERSION/bin/linux/amd64/kubectl -o ~/.local/bin/kubectl
chmod +x ~/.local/bin/kubectl

HELM_VERSION=$(curl -s https://api.github.com/repos/helm/helm/releases/latest | grep '"tag_name":' | sed -E 's/.*"tag_name": "([^"]+)".*/\1/')
curl -fsSL https://get.helm.sh/helm-$HELM_VERSION-linux-amd64.tar.gz | tar zxvf - -C "$HOME/.local/bin" linux-amd64/helm --strip-components 1

######################################################################################################

KUBECTX_VERSION=$(curl -s https://api.github.com/repos/ahmetb/kubectx/releases/latest | grep '"tag_name":' | sed -E 's/.*"tag_name": "([^"]+)".*/\1/')
curl -fsSL https://github.com/ahmetb/kubectx/releases/download/$KUBECTX_VERSION/kubectx -o ~/.local/bin/kubectx
chmod +x ~/.local/bin/kubectx

YQ_VERSION=$(curl -s https://api.github.com/repos/mikefarah/yq/releases/latest | grep '"tag_name":' | sed -E 's/.*"tag_name": "([^"]+)".*/\1/')
curl -fsSL https://github.com/mikefarah/yq/releases/download/$YQ_VERSION/yq_linux_amd64 -o ~/.local/bin/yq
chmod +x ~/.local/bin/yq

KUSTOMIZE_VERSION=$(curl -s https://api.github.com/repos/kubernetes-sigs/kustomize/releases/latest | grep '"tag_name":' | sed -E 's/.*"tag_name": "([^"]+)".*/\1/')
curl -fsSL https://github.com/kubernetes-sigs/kustomize/releases/download/${KUSTOMIZE_VERSION}/kustomize_${KUSTOMIZE_VERSION#kustomize/}_linux_amd64.tar.gz | tar zxvf - -C "$HOME/.local/bin"

######################################################################################################

cat <<'EOF'>> ~/.zshrc
source <(kubectl completion zsh)
alias k=kubectl
compdef k=kubectl
source <(helm completion zsh)
alias kx=kubectx
EOF

######################################################################################################

zsh

######################################################################################################

KIND_VERSION=$(curl -s https://api.github.com/repos/kubernetes-sigs/kind/releases/latest | grep '"tag_name":' | sed -E 's/.*"tag_name": "([^"]+)".*/\1/')
curl -fsSL https://github.com/kubernetes-sigs/kind/releases/download/$KIND_VERSION/kind-linux-amd64 -o ~/.local/bin/kind
chmod +x ~/.local/bin/kind

curl -fsSL https://raw.githubusercontent.com/WiltonCarvalho/kind-cicd/refs/heads/main/kind/kind-config.yaml -o ~/kind-config.yaml

kind delete cluster || true
docker network rm kind || true
docker network create kind --subnet 172.31.0.0/16

K8S_VERSION=$(kubectl version --client=true | grep Client | grep -Eo "v[0-9]+\.[0-9][0-9]")
K8S_VERSION=$(curl -fsSL 'https://registry.hub.docker.com/v2/repositories/kindest/node/tags?page=1&page_size=100' | yq -p json -r '.results[].name' | grep "^$K8S_VERSION" | sort -V | tail -1)
kind create cluster --image kindest/node:$K8S_VERSION --config ~/kind-config.yaml

######################################################################################################

helm upgrade --install metrics-server metrics-server --repo https://kubernetes-sigs.github.io/metrics-server --namespace kube-system \
--set "args={--kubelet-insecure-tls,--kubelet-preferred-address-types=InternalIP}"

METALLB_VERSION=$(curl -s https://api.github.com/repos/metallb/metallb/releases/latest | grep '"tag_name":' | sed -E 's/.*"tag_name": "([^"]+)".*/\1/')
kubectl apply -f https://raw.githubusercontent.com/metallb/metallb/$METALLB_VERSION/config/manifests/metallb-native.yaml
sleep 3
kubectl -n metallb-system wait --for=condition=ready pod -l app=metallb --timeout=60s

######################################################################################################

kubectl apply -f https://github.com/WiltonCarvalho/kubernetes/raw/main/kind-metal-lb-nginx-ingress/metallb-config.yaml
kubectl -n metallb-system get ipaddresspools

kubectl create deployment httpd --image httpd:alpine --port 80 --dry-run=client -o yaml > deployment.yaml
kubectl apply -f deployment.yaml
kubectl wait --for=condition=ready pod -l app=httpd

kubectl expose deployment httpd --name=httpd --type=LoadBalancer --port 80 \
--target-port 80 --load-balancer-ip=172.31.255.25 --labels app=httpd --dry-run=client -o yaml > service.yaml
kubectl apply -f service.yaml

sleep 3
curl 172.31.255.25

######################################################################################################

openssl req -x509 -nodes -days 3650 -new -subj "/CN=wiltoncarvalho.com" \
  -newkey rsa:2048 -keyout /tmp/key.pem -out /tmp/cert.pem

kubectl create secret tls default-ssl-secret \
  --key /tmp/key.pem --cert /tmp/cert.pem --namespace default

######################################################################################################

helm upgrade --install ingress-nginx ingress-nginx \
  --repo https://kubernetes.github.io/ingress-nginx \
  --namespace ingress-nginx --create-namespace \
  --version 4.12.1 \
  --set controller.service.type=LoadBalancer \
  --set controller.service.loadBalancerIP=172.31.255.254 \
  --set controller.extraArgs.default-ssl-certificate='default/default-ssl-secret'

######################################################################################################

kubectl -n ingress-nginx wait --for=condition=ready pod -l "app.kubernetes.io/name=ingress-nginx" --timeout=60s
sleep 3
curl -I 172.31.255.254/healthz

######################################################################################################

kubectl create ingress httpd --class=nginx --rule "httpd.wiltoncarvalho.com/*=httpd:80" --dry-run=client -o yaml > ingress.yaml
kubectl apply -f ingress.yaml
sleep 3
curl http://httpd.wiltoncarvalho.com --resolve httpd.wiltoncarvalho.com:80:172.31.255.254
curl https://httpd.wiltoncarvalho.com --resolve httpd.wiltoncarvalho.com:443:172.31.255.254 -k

######################################################################################################

kubectl apply -f kind/registry.yaml
kubectl wait --for=condition=ready pod -l app=registry
curl -i http://registry.172.31.255.201.sslip.io

skopeo copy --dest-tls-verify=false docker://nginx:alpine docker://registry.172.31.255.201.sslip.io/nginx:alpine

######################################################################################################

kubectl create deployment nginx --image registry.172.31.255.201.sslip.io/nginx:alpine --port 80
kubectl wait --for=condition=ready pod -l app=nginx
kubectl patch deployment nginx --type='json' -p='[{"op": "replace", "path": "/spec/template/spec/containers/0/image", "value":"localhost:32000/nginx:alpine"}]'
kubectl wait --for=condition=ready pod -l app=nginx

######################################################################################################

helm upgrade --install argocd argo-cd --repo https://argoproj.github.io/argo-helm \
--namespace argocd --values argocd/argocd-values.yaml --create-namespace

kubectl -n argocd wait --for=condition=ready pod -l "app.kubernetes.io/part-of=argocd"

kubectl -n argocd get secret argocd-initial-admin-secret -o jsonpath="{.data.password}" | base64 -d && echo

google-chrome http://argocd.172.31.255.254.sslip.io

######################################################################################################

kubectl apply -k gitea

google-chrome http://gitea.172.31.255.254.sslip.io

kubectl exec -it gitea-0 -- su-exec git /usr/local/bin/gitea admin user generate-access-token --username root --token-name root --scopes all

09c7aa2a5b64183bab99de4ddaca1d6ab009badb

(
  mkdir -p /tmp/whoami
  cp kind/whoami.yaml /tmp/whoami/
  cd /tmp/whoami
  echo '# whoami' > readme.md
  git config --global user.email "you@example.com"
  git config --global user.name "Your Name"
  git init --initial-branch=main
  git remote rm origin || true
  git remote add origin http://root:09c7aa2a5b64183bab99de4ddaca1d6ab009badb@gitea.172.31.255.254.sslip.io/root/whoami
  git add readme.md
  git add whoami.yaml
  git commit -m "Initial commit"
  git push --set-upstream origin main
)

Add Gitea token to argocd/argocd-creds.yaml

kubectl apply -f argocd/argocd-creds.yaml
kubectl apply -f argocd/argocd-application.yaml

google-chrome http://gitea.172.31.255.254.sslip.io/admin/applications

Create a Gitea Oauth2 App for Drone
Redirect URI: http://drone.172.31.255.254.sslip.io/login
Add Key and Secret to drone/drone.env

kubectl apply -k drone

google-chrome drone.172.31.255.254.sslip.io

Set the repo as trusted in Drone repo settings

